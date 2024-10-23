using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka;
using QuixStreams.Kafka.Transport;
using QuixStreams.Streaming.Configuration;
using QuixStreams.Streaming.Models;
using QuixStreams.Streaming.Raw;
using QuixStreams.Streaming.Utils;
using QuixStreams.Telemetry.Configuration;
using QuixStreams.Telemetry.Kafka;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;
using SaslMechanism = Confluent.Kafka.SaslMechanism;

namespace QuixStreams.Streaming
{
    public interface IKafkaStreamingClient
    {
        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        ITopicConsumer GetTopicConsumer(string topic, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null);

        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive incoming streams.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="options">The settings to use for committing</param>
        /// <param name="partitionOffset">The partition offset to start reading from</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        ITopicConsumer GetTopicConsumer(string topic, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null);
        
        /// <summary>
        /// Gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages. 
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        /// <returns>Instance of <see cref="IRawTopicConsumer"/></returns>
        IRawTopicConsumer GetRawTopicConsumer(string topic, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null);

        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages.  
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topic);

        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topic, Partition partition);

        /// <summary>
        /// Gets a topic producer capable of publishing non-quixstreams messages.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partitioner">Partitioner to produce messages with.</param>
        /// <returns>Instance of <see cref="IRawTopicProducer"/></returns>
        IRawTopicProducer GetRawTopicProducer(string topic, QuixPartitionerDelegate partitioner);

        /// <summary>
        /// Gets a topic producer capable of publishing stream messages. 
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topic);

        /// <summary>
        /// Gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partition">The partition to produce to.</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topic, Partition partition);
        
        /// <summary>
        /// Gets a topic producer capable of publishing stream messages.
        /// </summary>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partitioner">The partitioner to produce with.</param>
        /// <returns>Instance of <see cref="ITopicProducer"/></returns>
        ITopicProducer GetTopicProducer(string topic, StreamPartitionerDelegate partitioner);
    }

    /// <summary>
    /// A Kafka streaming client capable of creating topic consumer and producers.
    /// </summary>
    public class KafkaStreamingClient : IKafkaStreamingClient
    {
        private readonly ILogger logger = Logging.CreateLogger<KafkaStreamingClient>();
        private readonly string brokerAddress;
        private readonly Dictionary<string, string> brokerProperties;
        
        static KafkaStreamingClient()
        {
            // this will guarantee that the codec is set to default, without modifying
            var codec = CodecSettings.CurrentCodec;
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="KafkaStreamingClient"/>
        /// </summary>
        /// <param name="brokerAddress">Address of Kafka cluster.</param>
        /// <param name="securityOptions">Optional security options.</param>
        /// <param name="properties">Additional broker properties</param>
        /// <param name="debug">Whether debugging should be enabled</param>
        public KafkaStreamingClient(string brokerAddress, SecurityOptions securityOptions = null, IDictionary<string, string> properties = null, bool debug = false)
        {
            this.brokerAddress = brokerAddress;
            if (securityOptions == null)
            {
                this.brokerProperties = new Dictionary<string, string>();
            }
            else
            {
                var securityOptionsBuilder = new SecurityOptionsBuilder();

                if (securityOptions.UseSsl)
                {
                    if (!string.IsNullOrWhiteSpace(securityOptions.SslCaContent))
                    {
                        securityOptionsBuilder.SetSslCaContent(securityOptions.SslCaContent);
                    }
                    else
                    {
                        securityOptionsBuilder.SetSslEncryption(securityOptions.SslCertificates);
                    }
                }
                else
                {
                    securityOptionsBuilder.SetNoEncryption();
                }

                if (securityOptions.UseSasl)
                {
                    if (!Enum.TryParse(securityOptions.SaslMechanism.ToString(), true, out SaslMechanism parsed))
                    {
                        throw new ArgumentOutOfRangeException(nameof(securityOptions.SaslMechanism), "Unsupported sasl mechanism " + securityOptions.SaslMechanism);
                    }

                    securityOptionsBuilder.SetSaslAuthentication(securityOptions.Username, securityOptions.Password, parsed);
                }
                else
                {
                    securityOptionsBuilder.SetNoAuthentication();
                }

                this.brokerProperties = securityOptionsBuilder.Build();
            }

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    this.brokerProperties[property.Key] = property.Value;
                }
            }

            if (debug) this.brokerProperties["debug"] = "all";
        }
        
        /// <inheritdoc/>
        public ITopicConsumer GetTopicConsumer(string topic, string consumerGroup = null, CommitOptions options = null, AutoOffsetReset autoOffset = AutoOffsetReset.Latest, ICollection<Partition> partitions = null)
        {
            var kafkaReaderConfiguration = new TelemetryKafkaConsumerConfiguration(brokerAddress, consumerGroup, brokerProperties)
            {
                CommitOptions = options,
                AutoOffsetReset = autoOffset.ConvertToKafka()
            };

            var kafkaReader = new TelemetryKafkaConsumer(kafkaReaderConfiguration, topic, partitions: partitions);

            var topicConsumer = new TopicConsumer(kafkaReader);

            App.Register(topicConsumer);

            return topicConsumer;
        }

        /// <inheritdoc/>
        public ITopicConsumer GetTopicConsumer(string topic, PartitionOffset partitionOffset, string consumerGroup = null, CommitOptions options = null)
        {
            var kafkaReaderConfiguration = new TelemetryKafkaConsumerConfiguration(brokerAddress, consumerGroup, brokerProperties)
            {
                CommitOptions = options
            };

            var kafkaReader = new TelemetryKafkaConsumer(kafkaReaderConfiguration, topic, partitionOffset);

            var topicConsumer = new TopicConsumer(kafkaReader);

            App.Register(topicConsumer);

            return topicConsumer;
        }

        /// <inheritdoc/>
        public IRawTopicConsumer GetRawTopicConsumer(string topic, string consumerGroup = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null)
        {
            var rawTopicConsumer = new RawTopicConsumer(brokerAddress, topic, consumerGroup, brokerProperties, autoOffset ?? AutoOffsetReset.Latest, partitions: partitions);

            App.Register(rawTopicConsumer);
            
            return rawTopicConsumer;
        }

        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topic)
        {
            var rawTopicProducer = new RawTopicProducer(brokerAddress, topic, brokerProperties);

            App.Register(rawTopicProducer);

            return rawTopicProducer;
        }

        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topic, Partition partition)
        {
            var rawTopicProducer = new RawTopicProducer(brokerAddress, topic, brokerProperties, partition);

            App.Register(rawTopicProducer);

            return rawTopicProducer;
        }
        
        /// <inheritdoc/>
        public IRawTopicProducer GetRawTopicProducer(string topic, QuixPartitionerDelegate partitioner)
        {
            var rawTopicProducer = new RawTopicProducer(brokerAddress, topic, brokerProperties, partitioner);

            App.Register(rawTopicProducer);

            return rawTopicProducer;
        }
        
        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topic)
        {
            var topicProducer = new TopicProducer(new KafkaProducerConfiguration(brokerAddress, brokerProperties), topic);
            
            App.Register(topicProducer);

            return topicProducer;
        }

        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topic, Partition partition)
        {
            var topicProducer = new TopicProducer(new KafkaProducerConfiguration(brokerAddress, brokerProperties), topic, partition);

            App.Register(topicProducer);

            return topicProducer;
        }
        
        /// <inheritdoc/>
        public ITopicProducer GetTopicProducer(string topic, StreamPartitionerDelegate partitioner)
        {
            var topicProducer = new TopicProducer(new KafkaProducerConfiguration(brokerAddress, brokerProperties), topic, partitioner);

            App.Register(topicProducer);

            return topicProducer;
        }
    }

    /// <summary>
    /// Extensions for Streaming Client class
    /// </summary>
    public static class KafkaStreamingClientExtensions
    {
        /// <summary>
        /// Open an topic consumer capable of subscribing to receive incoming streams
        /// </summary>
        /// <param name="client">Streaming Client instance</param>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="commitMode">The commit strategy to use for this topic</param>
        /// <returns>Instance of <see cref="ITopicConsumer"/></returns>
        public static ITopicConsumer GetTopicConsumer(this IKafkaStreamingClient client, string topic, string consumerGroup = null, CommitMode commitMode = CommitMode.Automatic, AutoOffsetReset autoOffset =  AutoOffsetReset.Latest)
        {
            switch (commitMode)
            {
                case CommitMode.Automatic:
                    return client.GetTopicConsumer(topic, consumerGroup, autoOffset: autoOffset);
                case CommitMode.Manual:
                    var commitOptions = new CommitOptions()
                    {
                        AutoCommitEnabled = false
                    };
                    return client.GetTopicConsumer(topic, consumerGroup, commitOptions, autoOffset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(commitMode), commitMode, null);
            }
        }
    }
}
