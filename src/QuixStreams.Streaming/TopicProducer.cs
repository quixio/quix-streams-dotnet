using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka;
using QuixStreams.Telemetry.Kafka;

namespace QuixStreams.Streaming
{
    /// <summary>
    /// Implementation of <see cref="ITopicProducer"/> to produce outgoing streams
    /// </summary>
    public class TopicProducer : ITopicProducerInternal
    {
        private readonly string topic;
        private readonly Func<string, TelemetryKafkaProducer> createKafkaProducer;
        private readonly ConcurrentDictionary<string, Lazy<IStreamProducer>> streams = new ConcurrentDictionary<string, Lazy<IStreamProducer>>();
        private readonly IKafkaProducer kafkaProducer;
        private readonly ILogger<TopicProducer> logger = Logging.CreateLogger<TopicProducer>();
        private bool disposed = false;

        /// <inheritdoc />
        public event EventHandler OnDisposed;
        
        /// <summary>
        /// Initializes a new instance of <see cref="TopicProducer"/>
        /// </summary>
        /// <param name="createKafkaProducer">Function factory to create a Kafka producer from Telemetry layer.</param>
        public TopicProducer(Func<string, TelemetryKafkaProducer> createKafkaProducer)
        {
            this.createKafkaProducer = createKafkaProducer;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicProducer"/> class.
        /// </summary>
        /// <param name="config">Kafka producer configuration.</param>
        /// <param name="topic">Name of the topic.</param>
        public TopicProducer(KafkaProducerConfiguration config, string topic)
            : this(config, topic, Partition.Any)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicProducer"/> class.
        /// </summary>
        /// <param name="config">Kafka producer configuration.</param>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partition">Partition to produce to.</param>
        public TopicProducer(KafkaProducerConfiguration config, string topic, Partition partition)
            : this(config, topic, partition, null)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicProducer"/> class.
        /// </summary>
        /// <param name="config">Kafka producer configuration.</param>
        /// <param name="topic">Name of the topic.</param>
        /// <param name="partitioner">Partitioner to produce each message with.</param>
        public TopicProducer(KafkaProducerConfiguration config, string topic, StreamPartitionerDelegate partitioner)
            : this(config, topic, Partition.Any, partitioner)

        {
        }
        
        private TopicProducer(KafkaProducerConfiguration config, string topic, Partition partition, StreamPartitionerDelegate partitioner)
        {
            this.topic = topic;

            var prodConfig = new ProducerConfiguration(config.BrokerList, config.Properties);
            var topicConfig = partitioner == null
                ? new ProducerTopicConfiguration(topic, partition)
                : new ProducerTopicConfiguration(topic, 
                    (partitionerTopic, partitionCount, message) =>
                        partitioner(partitionerTopic, message.Key == null ? null : Encoding.UTF8.GetString(message.Key), partitionCount));
            
            this.kafkaProducer =  new KafkaProducer(prodConfig, topicConfig);

            createKafkaProducer = (string streamId) => new TelemetryKafkaProducer(this.kafkaProducer, streamId);
        }

        /// <inheritdoc />
        public IStreamProducer CreateStream()
        {
            var streamProducer = new StreamProducer(this, createKafkaProducer);

            if (!this.streams.TryAdd(streamProducer.StreamId, new Lazy<IStreamProducer>(() => streamProducer)))
            {
                throw new Exception($"A stream with id '{streamProducer.StreamId}' already exists in the managed list of streams of the Topic producer.");
            }

            return streamProducer;
        }

        /// <inheritdoc />
        public IStreamProducer CreateStream(string streamId)
        {
            var stream = this.streams.AddOrUpdate(streamId, 
                (id) => new Lazy<IStreamProducer>(() => new StreamProducer(this, createKafkaProducer, streamId)),
                (id, s) => throw new Exception($"A stream with id '{streamId}' already exists in the managed list of streams of the Topic producer."));

            return stream.Value;
        }

        /// <inheritdoc />
        public IStreamProducer GetStream(string streamId)
        {
            if (!this.streams.TryGetValue(streamId, out var stream))
            {
                return null;
            }

            return stream.Value;
        }

        /// <inheritdoc />
        public IStreamProducer GetOrCreateStream(string streamId, Action<IStreamProducer> onStreamCreated = null)
        {
            var stream = this.streams.GetOrAdd(streamId, id =>
            {
                return new Lazy<IStreamProducer>(() =>
                {
                    var createdStream = new StreamProducer(this, createKafkaProducer, streamId);
                    onStreamCreated?.Invoke(createdStream);
                    return createdStream;
                });
            });

            return stream.Value;
        }

        /// <inheritdoc />
        public void RemoveStream(string streamId)
        {
            this.streams.TryRemove(streamId, out var stream);
        }
        
        /// <inheritdoc />
        public void Flush()
        {
            this.logger.LogTrace("Flushing topic {0}", topic);
            var activeStreams = this.streams.ToList();
            foreach (var stream in activeStreams)
            {
                this.logger.LogTrace("Flushing stream {0} for topic {1}", stream.Key, topic);
                // All should be evaluated at this point, as lazy is mainly there to avoid creating new
                // if already exists in a thread-safe manner, but if got added it'll be evaluated
                stream.Value.Value.Flush(); 
            }
            this.kafkaProducer?.Flush(default);
            this.logger.LogTrace("Flushed topic {0}", topic);

        }

        /// <summary>
        /// Flushes pending data to the broker and disposes underlying resources
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            this.Flush();
            this.kafkaProducer?.Dispose();
            this.OnDisposed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Calculate a partition number given a <paramref name="partitionCount" />
    ///     and <paramref name="streamId" />. The <paramref name="topic" />
    ///     is also provided, but is typically not used.
    /// </summary>
    /// <remarks>
    ///     A partitioner instance may be called in any thread at any time and
    ///     may be called multiple times for the same message/key.
    /// 
    ///     A partitioner:
    ///     - MUST NOT block or execute for prolonged periods of time.
    ///     - MUST return a value between 0 and partitionCount-1.
    ///     - MUST NOT throw any exception.
    /// </remarks>
    /// <param name="topic">The topic.</param>
    /// <param name="partitionCount">
    ///     The number of partitions in <paramref name="topic" />.
    /// </param>
    /// <param name="streamId">The stream id the message will be produced with.</param>
    /// <returns>
    ///     The calculated <seealso cref="T:Confluent.Kafka.Partition" />, possibly
    ///     <seealso cref="F:Confluent.Kafka.Partition.Any" />.
    /// </returns>
    public delegate Partition StreamPartitionerDelegate(string topic, string streamId, int partitionCount);
}
