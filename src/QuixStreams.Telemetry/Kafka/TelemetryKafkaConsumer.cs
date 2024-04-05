using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka;
using QuixStreams.Kafka.Transport;

namespace QuixStreams.Telemetry.Kafka
{
    /// <summary>
    /// KafkaReader initializes transport layer classes and sets up a <see cref="StreamPipelineFactory"/> to detect new streams in Kafka topic 
    /// and start new <see cref="StreamPipeline"/> instances where all the messages of the stream are going to be sent.
    /// </summary>
    public class TelemetryKafkaConsumer : IDisposable
    {
        /// <summary>
        /// The topic the kafka consumer is created for
        /// </summary>
        public readonly string Topic = "Unknown";

        private readonly ILogger logger = Logging.CreateLogger<TelemetryKafkaConsumer>();
        private IKafkaTransportConsumer kafkaTransportConsumer;
        private bool isDisposed = false;

        private StreamPipelineFactory streamPipelineFactory;
        private Func<string, IStreamPipeline> streamPipelineFactoryHandler;
        private IKafkaConsumer kafkaConsumer;
        private readonly Action<CommitOptions> configureCommitOptions;

        /// <summary>
        /// Event raised when an exception occurs during the Reading processes
        /// </summary>
        public event EventHandler<Exception> OnReceiveException;

        /// <summary>
        /// Event raised with streams belonging to kafka partition(s) revoked
        /// </summary>
        public event Action<IStreamPipeline[]> OnStreamsRevoked;

        /// <summary>
        /// Raised when the kafka topic will became unavailable, but it is still possible at this point
        /// </summary>
        public event EventHandler OnRevoking;

        /// <summary>
        /// Raised when commit occurs
        /// </summary>
        public event EventHandler OnCommitted;

        /// <summary>
        /// Raised before commit
        /// </summary>
        public event EventHandler OnCommitting;

        /// <summary>
        /// Stream Context cache for all the streams of the topic
        /// </summary>
        public IStreamContextCache ContextCache { get; private set; }

        /// <summary>
        /// Group id
        /// </summary>
        public readonly string GroupId;

       /// <summary>
        /// Initializes a new instance of <see cref="TelemetryKafkaConsumer"/>
        /// </summary>
        /// <param name="telemetryKafkaConsumerConfiguration">Kafka broker configuration for <see cref="TelemetryKafkaConsumer"/></param>
        /// <param name="topic">Topic name to read from</param>
        /// <param name="verifyBrokerConnection">If set to false it wont wait for the broker verification</param>
        public TelemetryKafkaConsumer(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration, string topic, bool verifyBrokerConnection = true)
            :this(telemetryKafkaConsumerConfiguration, ConvertConfigurationHelper(telemetryKafkaConsumerConfiguration, out var subConfig), topic)
        {
            var topicConfig = new ConsumerTopicConfiguration(topic);
            this.kafkaConsumer = new KafkaConsumer(subConfig, topicConfig)
            {
                VerifyBrokerConnection = verifyBrokerConnection
            };
        }
       
       /// <summary>
       /// Initializes a new instance of <see cref="TelemetryKafkaConsumer"/>
       /// </summary>
       /// <param name="telemetryKafkaConsumerConfiguration">Kafka broker configuration for <see cref="TelemetryKafkaConsumer"/></param>
       /// <param name="topic">Topic name to read from</param>
       /// <param name="partitionOffset">The partition offset to start reading from</param>
       /// <param name="verifyBrokerConnection">If set to false it wont wait for the broker verification</param>
       [Obsolete("Use constructor supporting multiple partition offsets")]
       public TelemetryKafkaConsumer(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration, string topic, PartitionOffset partitionOffset, bool verifyBrokerConnection = true)
           :this(telemetryKafkaConsumerConfiguration, topic, new [] {partitionOffset}, verifyBrokerConnection)
       {
       }
        
        /// <summary>
        /// Initializes a new instance of <see cref="TelemetryKafkaConsumer"/>
        /// </summary>
        /// <param name="telemetryKafkaConsumerConfiguration">Kafka broker configuration for <see cref="TelemetryKafkaConsumer"/></param>
        /// <param name="topic">Topic name to read from</param>
        /// <param name="partitionOffsets">The partition offsets to start reading from</param>
        /// <param name="verifyBrokerConnection">If set to false it wont wait for the broker verification</param>
        public TelemetryKafkaConsumer(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration, string topic, ICollection<PartitionOffset> partitionOffsets, bool verifyBrokerConnection = true)
            :this(telemetryKafkaConsumerConfiguration, ConvertConfigurationHelper(telemetryKafkaConsumerConfiguration, out var subConfig), topic)
        {
            var topicConfig = new ConsumerTopicConfiguration(topic, partitionOffsets);
            this.kafkaConsumer = new KafkaConsumer(subConfig, topicConfig)
            {
                VerifyBrokerConnection = verifyBrokerConnection
            };
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="TelemetryKafkaConsumer"/>
        /// </summary>
        /// <param name="telemetryKafkaConsumerConfiguration">Kafka broker configuration for <see cref="TelemetryKafkaConsumer"/></param>
        /// <param name="topic">Topic name to read from</param>
        /// <param name="partitions">The partitions to start reading from</param>
        /// <param name="verifyBrokerConnection">If set to false it wont wait for the broker verification</param>
        public TelemetryKafkaConsumer(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration, string topic, ICollection<Partition> partitions, bool verifyBrokerConnection = true)
        : this(telemetryKafkaConsumerConfiguration, ConvertConfigurationHelper(telemetryKafkaConsumerConfiguration, out var subConfig), topic)
        {
            var topicConfig = new ConsumerTopicConfiguration(topic, partitions);
            this.kafkaConsumer = new KafkaConsumer(subConfig, topicConfig)
            {
                VerifyBrokerConnection = verifyBrokerConnection
            };
        }
        
        private TelemetryKafkaConsumer(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration, ConsumerConfiguration consumerConfiguration, string topic)
        {
            Topic = topic;
            // Kafka Transport layer -> Transport layer
            var subConfig = telemetryKafkaConsumerConfiguration.ToSubscriberConfiguration();
            var commitOptions = telemetryKafkaConsumerConfiguration.CommitOptions ?? new CommitOptions();
            if (commitOptions.AutoCommitEnabled && !consumerConfiguration.ConsumerGroupSet)
            {
                logger.LogDebug("Disabled automatic kafka commit as no consumer group is set");
                commitOptions.AutoCommitEnabled = false;
            }

            this.configureCommitOptions = o =>
            {
                o.CommitEvery = commitOptions.CommitEvery;
                o.CommitInterval = commitOptions.CommitInterval;
                o.AutoCommitEnabled = commitOptions.AutoCommitEnabled;
            };
            
            this.GroupId = subConfig.GroupId;
        }

        /// <summary>
        /// Main purpose is to avoid saving it on instance and converting twice in constructors
        /// </summary>
        private static ConsumerConfiguration ConvertConfigurationHelper(TelemetryKafkaConsumerConfiguration telemetryKafkaConsumerConfiguration,
            out ConsumerConfiguration consumerConfiguration)
        {
            // The out parameter is to use in invoking constructor
            consumerConfiguration = telemetryKafkaConsumerConfiguration.ToSubscriberConfiguration();
            // The return is to pass to the initial constructor
            return consumerConfiguration;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TelemetryKafkaConsumer"/>
        /// </summary>
        /// <param name="consumer">The consumer to use</param>
        /// <param name="commitOptions">The commit options to use</param>
        public TelemetryKafkaConsumer(IKafkaConsumer consumer, CommitOptions commitOptions)
        {
            this.kafkaConsumer = consumer;

            if (commitOptions == null) commitOptions = new CommitOptions();
            this.configureCommitOptions = o =>
            {
                o.CommitEvery = commitOptions.CommitEvery;
                o.CommitInterval = commitOptions.CommitInterval;
                o.AutoCommitEnabled = commitOptions.AutoCommitEnabled;
            };
        }

        private bool InitializeTransport()
        {
            if (kafkaTransportConsumer != null)
                return false;

            this.kafkaConsumer.OnErrorOccurred += ReadingExceptionHandler;
            this.kafkaTransportConsumer = new KafkaTransportConsumer(kafkaConsumer, (o) => { this.configureCommitOptions?.Invoke(o.CommitOptions); });
            return true;
        }

        private void ReadingExceptionHandler(object sender, Exception e)
        {
            if (this.OnReceiveException == null)
            {
                if (e is KafkaException)
                {
                    this.logger.LogError(e, "Exception receiving package from Kafka");
                }
                else
                {
                    this.logger.LogError(e, "Exception while processing package");
                }
            }
            else
            {
                this.OnReceiveException?.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Starts reading from Kafka.
        /// </summary>
        public void Start()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(TelemetryKafkaConsumer));
            if (!this.InitializeTransport()) return;
            // Transport layer -> Streaming layer
            ContextCache = new StreamContextCache();
            this.streamPipelineFactory = new StreamPipelineFactory(this.kafkaTransportConsumer, streamPipelineFactoryHandler, ContextCache);
            this.streamPipelineFactory.OnStreamsRevoked += StreamsRevokedHandler;
            this.kafkaTransportConsumer.OnRevoking += RevokingHandler;
            this.kafkaTransportConsumer.OnCommitted += CommittedHandler;
            this.kafkaTransportConsumer.OnCommitting += CommitingHandler;
            this.streamPipelineFactory.Open();
            this.kafkaConsumer.Open();
        }

        private void CommittedHandler(object sender, CommittedEventArgs e)
        {
            this.OnCommitted?.Invoke(this, EventArgs.Empty);
        }

        private void CommitingHandler(object sender, CommittingEventArgs e)
        {
            this.OnCommitting?.Invoke(this, EventArgs.Empty);
        }

        private void RevokingHandler(object sender, RevokingEventArgs e)
        {
            this.OnRevoking?.Invoke(this, EventArgs.Empty);
        }

        private void StreamsRevokedHandler(IStreamPipeline[] obj)
        {
            OnStreamsRevoked?.Invoke(obj);
        }

        /// <summary>
        /// Function to execute for each stream received by the reader.
        /// </summary>
        /// <param name="streamPipelineFactoryHandler">Handler factory to execute for each Stream detected in the incoming messages in order 
        /// to create a new <see cref="StreamPipeline"/> for each stream. 
        /// The handler function receives a StreamId and has to return a <see cref="StreamPipeline"/>.</param>
        public void ForEach(Func<string, IStreamPipeline> streamPipelineFactoryHandler)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(TelemetryKafkaConsumer));
            this.streamPipelineFactoryHandler = streamPipelineFactoryHandler;
        }

        /// <summary>
        /// Stops reading from Kafka and closes all the Stream pipelinees.
        /// </summary>
        public void Stop()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(TelemetryKafkaConsumer));
            StopHelper();
        }

        private void StopHelper()
        {
            // Transport layer
            if (kafkaTransportConsumer != null)
            {
                this.kafkaTransportConsumer.OnRevoking -= RevokingHandler;
                this.kafkaTransportConsumer.OnCommitted -= CommittedHandler;
                this.kafkaTransportConsumer.Dispose();
                this.kafkaTransportConsumer = null;
            }

            this.kafkaConsumer?.Close();

            // Stream pipeline factory
            if (this.streamPipelineFactory != null)
            {
                this.streamPipelineFactory.Close();
                this.streamPipelineFactory.OnStreamsRevoked -= this.StreamsRevokedHandler;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            this.StopHelper();

            this.kafkaConsumer = null;
            this.kafkaTransportConsumer = null;
            this.streamPipelineFactory = null;
        }

        /// <summary>
        /// Commit packages read from kafka up to this point
        /// </summary>
        public void Commit()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(TelemetryKafkaConsumer));
            if (kafkaTransportConsumer == null) throw new InvalidOperationException("Not able to commit to inactive reader");
            Debug.Assert(ContextCache != null);
            lock (this.ContextCache.Sync)
            {
                this.logger.LogTrace("Starting manual commit");
                var all = this.ContextCache.GetAll();
                var contexts = all.Select(y => y.Value.LastUncommittedTopicPartitionOffset).Where(y => y != null).ToArray();
                if (contexts.Length == 0)
                {
                    this.logger.LogTrace("Finished manual commit (nothing to commit)");
                    return; // there is nothing to commit
                }

                this.kafkaTransportConsumer.Commit(contexts);
                this.logger.LogTrace("Finished manual commit");
            }
        }
    }
}