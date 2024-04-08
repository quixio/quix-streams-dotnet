using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka;

namespace QuixStreams.Streaming.Raw
{
    /// <summary>
    /// Class to produce raw messages into a Topic (capable to producing non-quixstreams messages)
    /// </summary>
    public class RawTopicProducer: IRawTopicProducer
    {
        private string topicName;
        private readonly ILogger logger = Logging.CreateLogger<RawTopicProducer>();
        private readonly IKafkaProducer kafkaProducer = null;
        private bool disposed = false;
        
        /// <inheritdoc />
        public event EventHandler OnDisposed;

        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicProducer"/>
        /// </summary>
        /// <param name="brokerAddress">Address of Kafka cluster.</param>
        /// <param name="topicName">Name of the topic.</param>
        /// <param name="brokerProperties">Additional broker properties</param>
        public RawTopicProducer(string brokerAddress, string topicName, Dictionary<string, string> brokerProperties = null)
            : this(brokerAddress, topicName, brokerProperties, Partition.Any)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicProducer"/>
        /// </summary>
        /// <param name="brokerAddress">Address of Kafka cluster.</param>
        /// <param name="topicName">Name of the topic.</param>
        /// <param name="partition">Partition to produce to.</param>
        /// <param name="brokerProperties">Additional broker properties</param>
        public RawTopicProducer(string brokerAddress, string topicName, Dictionary<string, string> brokerProperties, Partition partition)
            : this(brokerAddress, topicName, brokerProperties, partition, null)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicProducer"/>
        /// </summary>
        /// <param name="brokerAddress">Address of Kafka cluster.</param>
        /// <param name="topicName">Name of the topic.</param>
        /// <param name="partitioner">Partitioner to produce each message with.</param>
        /// <param name="brokerProperties">Additional broker properties</param>
        public RawTopicProducer(string brokerAddress, string topicName, Dictionary<string, string> brokerProperties, QuixPartitionerDelegate partitioner)
            : this(brokerAddress, topicName, brokerProperties, Partition.Any, partitioner)
        {
        }
        
        private RawTopicProducer(string brokerAddress, string topicName, Dictionary<string, string> brokerProperties, Partition partition, QuixPartitionerDelegate partitioner)
        {
            brokerProperties ??= new Dictionary<string, string>();
            if (!brokerProperties.ContainsKey("queued.max.messages.kbytes")) brokerProperties["queued.max.messages.kbytes"] = "20480";

            this.topicName = topicName;

            var publisherConfiguration = new ProducerConfiguration(brokerAddress, brokerProperties);
            var topicConfiguration = partitioner == null
                ? new ProducerTopicConfiguration(this.topicName, partition)
                : new ProducerTopicConfiguration(this.topicName, partitioner);

            this.kafkaProducer = new KafkaProducer(publisherConfiguration, topicConfiguration);
        }
        
        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicProducer"/>
        /// </summary>
        /// <param name="kafkaProducer">The kafka producer to use</param>
        /// <param name="topicName">The optional topic name to use</param>
        public RawTopicProducer(IKafkaProducer kafkaProducer, string topicName = null)
        {
            this.topicName = topicName ?? "Unknown";
            this.kafkaProducer = kafkaProducer;
        }

        /// <inheritdoc />
        public void Publish(KafkaMessage message)
        {
            kafkaProducer.Publish(message);
        }
        
        /// <inheritdoc />
        public void Flush()
        {
            this.logger.LogTrace("Flushing topic {1}", this.topicName);
            this.kafkaProducer?.Flush(default);
            this.logger.LogTrace("Flushed topic {1}", this.topicName);
        }

        /// <summary>
        /// Flushes pending messages and disposes underlying resources
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
}