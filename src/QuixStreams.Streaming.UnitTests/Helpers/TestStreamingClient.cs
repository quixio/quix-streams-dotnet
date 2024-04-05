using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using QuixStreams.Kafka;
using QuixStreams.Kafka.Transport;
using QuixStreams.Kafka.Transport.Tests.Helpers;
using QuixStreams.Streaming.Raw;
using QuixStreams.Streaming.Utils;
using QuixStreams.Telemetry.Kafka;
using QuixStreams.Telemetry.Models;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;

namespace QuixStreams.Streaming.UnitTests.Helpers
{
    public class TestStreamingClient : IQuixStreamingClient, IKafkaStreamingClient
    {
        private readonly TimeSpan publishDelay;
        private TelemetryKafkaConsumer telemetryKafkaConsumer;
        private Func<string, TelemetryKafkaProducer> createKafkaProducer;
        private Dictionary<string, TestBroker> brokers = new Dictionary<string, TestBroker>();

        public TestStreamingClient(CodecType codec = CodecType.Protobuf, TimeSpan publishDelay = default)
        {
            this.publishDelay = publishDelay;
            CodecSettings.SetGlobalCodecType(codec);
        }

        public ITopicConsumer GetTopicConsumer()
        {
            return GetTopicConsumer("DEFAULT");
        }

        public ITopicConsumer GetTopicConsumer(string topic)
        {
            var broker = GetBroker(topic);
            this.telemetryKafkaConsumer = new TelemetryKafkaConsumer(broker, null);

            var topicConsumer = new TopicConsumer(this.telemetryKafkaConsumer);

            return topicConsumer;
        }
        
        public ITopicProducer GetTopicProducer()
        {
            return GetTopicProducer("DEFAULT");
        }   

        public IRawTopicProducer GetRawTopicProducer(string topic, QuixPartitionerDelegate partitioner)
        {
            return GetRawTopicProducer(topic);
        }

        public ITopicProducer GetTopicProducer(string topic)
        {
            var broker = GetBroker(topic);
            this.createKafkaProducer = streamId => new TelemetryKafkaProducer(broker, streamId);

            var topicProducer = new TopicProducer(this.createKafkaProducer);

            return topicProducer;
        }

        public ITopicProducer GetTopicProducer(string topic, int partitionId)
        {
            return GetTopicProducer(topic);
        }

        private TestBroker GetBroker(string topic)
        {
            if (this.brokers.TryGetValue(topic, out var broker)) return broker;
            broker = new TestBroker((msg) => Task.Delay(publishDelay));
            this.brokers[topic] = broker;
            return broker;
        }
        
        public IRawTopicConsumer GetRawTopicConsumer(string topic, string consumerGroup, AutoOffsetReset? autoOffset, ICollection<Partition> partitions = null)
        {
            throw new NotImplementedException();
        }

        public IRawTopicProducer GetRawTopicProducer(string topic)
        {
            throw new NotImplementedException();
        }

        public IRawTopicProducer GetRawTopicProducer(string topic, Partition partition)
        {
            return GetRawTopicProducer(topic);
        }

        public ITopicProducer GetTopicProducer(string topic, Partition partition)
        {
            return GetTopicProducer(topic);
        }

        public ITopicProducer GetTopicProducer(string topic, StreamPartitionerDelegate partitioner)
        {
            return GetTopicProducer(topic);
        }

        public ITopicConsumer GetTopicConsumer(string topic, string consumerGroup, CommitOptions options, AutoOffsetReset autoOffset, ICollection<Partition> partitions)
        {
            return GetTopicConsumer(topic);
        }
        
        public ITopicConsumer GetTopicConsumer(string topic, PartitionOffset partitionOffset, string consumerGroup, CommitOptions options)
        {
            return GetTopicConsumer(topic);
        }

        public Task<ITopicConsumer> GetTopicConsumerAsync(
            string topicIdOrName,
            string consumerGroup,
            CommitOptions options,
            AutoOffsetReset autoOffset,
            ICollection<Partition> partitions)
        {
            return Task.FromResult(GetTopicConsumer(topicIdOrName, consumerGroup, options, autoOffset, partitions));
        }
        
        
        public Task<ITopicConsumer> GetTopicConsumerAsync(
            string topicIdOrName,
            PartitionOffset offset,
            string consumerGroup,
            CommitOptions options)
        {
            return Task.FromResult(GetTopicConsumer(topicIdOrName, offset, consumerGroup, options));
        }

        public Task<IRawTopicConsumer> GetRawTopicConsumerAsync(string topicIdOrName, string consumerGroup, AutoOffsetReset? autoOffset, ICollection<Partition> partitions)
        {
            return Task.FromResult(GetRawTopicConsumer(topicIdOrName, consumerGroup, autoOffset));
        }

        public Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName)
        {
            return Task.FromResult(GetRawTopicProducer(topicIdOrName));
        }

        public Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, Partition partition)
        {
            return Task.FromResult(GetRawTopicProducer(topicIdOrName, partition));
        }

        public Task<IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName, QuixPartitionerDelegate partitioner)
        {
            return Task.FromResult(GetRawTopicProducer(topicIdOrName, partitioner));
        }

        public Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName)
        {
            return Task.FromResult(GetTopicProducer(topicIdOrName));
        }

        public Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, Partition partition)
        {
            return Task.FromResult(GetTopicProducer(topicIdOrName, partition));
        }

        public Task<ITopicProducer> GetTopicProducerAsync(string topicIdOrName, StreamPartitionerDelegate partitioner)
        {
            return Task.FromResult(GetTopicProducer(topicIdOrName, partitioner));
        }
    }
}
