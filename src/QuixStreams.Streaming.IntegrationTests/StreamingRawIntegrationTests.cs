using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Quix.TestBase.Extensions;
using QuixStreams.Kafka;
using QuixStreams;
using Xunit;
using Xunit.Abstractions;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;

namespace QuixStreams.Streaming.IntegrationTests
{
    [Collection("Kafka Container Collection")]
    public class StreamingRawIntegrationTests
    {
        private readonly ITestOutputHelper output;
        private readonly KafkaDockerTestFixture kafkaDockerTestFixture;
        private readonly KafkaStreamingClient client;

        public StreamingRawIntegrationTests(ITestOutputHelper output, KafkaDockerTestFixture kafkaDockerTestFixture)
        {
            this.output = output;
            this.kafkaDockerTestFixture = kafkaDockerTestFixture;
            QuixStreams.Logging.Factory = output.CreateLoggerFactory();
            client = new KafkaStreamingClient(kafkaDockerTestFixture.BrokerList, null);
        }

        [Fact]
        public async Task StreamRawWithoutKey_ShouldReadExpected()
        {
            var topicName = "streaming-raw-integration-test";
            await this.kafkaDockerTestFixture.EnsureTopic(topicName, 1);

            var topicConsumer = client.GetRawTopicConsumer(topicName, "Default", AutoOffsetReset.Earliest);

            var toSend = new byte[] { 1, 2, 0, 4, 6, 123, 54, 2 };
            var received = new List<byte[]>();


            topicConsumer.OnMessageReceived += (sender, message) =>
            {
                received.Add(message.Value);
            };

            topicConsumer.Subscribe();

            var topicProducer = client.GetRawTopicProducer(topicName);
            topicProducer.Publish(new KafkaMessage(null, toSend, null));
            SpinWait.SpinUntil(() => received.Count > 0, 10000);

            output.WriteLine($"received {received.Count} items");
            Assert.Single(received);
            Assert.Equal(toSend, received[0]);


            topicConsumer.Dispose();
            topicProducer.Dispose();
        }
        
        [Fact]
        public async Task StreamWithKey_ShouldReadExpected()
        {
            var topicName = "streaming-raw-integration-test2";
            await this.kafkaDockerTestFixture.EnsureTopic(topicName, 1);
            
            Thread.Sleep(5000); // This is only necessary because the container we use for kafka and how a topic creation is handled for the unit test

            var topicConsumer = client.GetRawTopicConsumer(topicName, "Default", AutoOffsetReset.Earliest);

            var toSend = new byte[] { 1, 2, 0, 4, 6, 123, 54, 2 };
            var received = new List<byte[]>();


            topicConsumer.OnMessageReceived += (sender, message) =>
            {
                received.Add(message.Value);
            };

            topicConsumer.Subscribe();
            var topicProducer = client.GetRawTopicProducer(topicName);
            topicProducer.Publish(new KafkaMessage(null, toSend, null));

            SpinWait.SpinUntil(() => received.Count > 0, 5000);

            Assert.Single(received);
            Assert.Equal(toSend, received[0]);


            topicConsumer.Dispose();
            topicProducer.Dispose();
        }
        
        [Fact]
        public async Task StreamToPartition_ShouldReadExpected()
        {
            var topicName = "StreamToPartition_ShouldReadExpected";
            await this.kafkaDockerTestFixture.EnsureTopic(topicName, 16);
            
            Thread.Sleep(5000); // This is only necessary because the container we use for kafka and how a topic creation is handled for the unit test

            var topicConsumer = client.GetRawTopicConsumer(topicName, "Default", AutoOffsetReset.Earliest);

            var toSend = new byte[] { 1, 2, 0, 4, 6, 123, 54, 2 };
            var received = new List<KafkaMessage>();


            topicConsumer.OnMessageReceived += (sender, message) =>
            {
                received.Add(message);
            };

            topicConsumer.Subscribe();
            var topicProducer = client.GetRawTopicProducer(topicName, new Partition(11));
            topicProducer.Publish(new KafkaMessage(null, toSend, null));
            var random = new Random();
            for (var ii = 0; ii < 100; ii++)
            {
                var key = new byte[64];
                random.NextBytes(key);
                topicProducer.Publish(new KafkaMessage(key, toSend, null));
            }


            SpinWait.SpinUntil(() => received.Count == 101, 5000);

            received.Count.Should().Be(101);
            received.All(y => y.TopicPartitionOffset.Partition.Value == 11).Should().BeTrue();


            topicConsumer.Dispose();
            topicProducer.Dispose();
        }
        
        [Fact]
        public async Task StreamWithPartitioner_ShouldReadExpected()
        {
            var topicName = "StreamWithPartitioner_ShouldReadExpected";
            await this.kafkaDockerTestFixture.EnsureTopic(topicName, 16);
            
            Thread.Sleep(5000); // This is only necessary because the container we use for kafka and how a topic creation is handled for the unit test

            var topicConsumer = client.GetRawTopicConsumer(topicName, "Default", AutoOffsetReset.Earliest);

            var toSend = new byte[] { 1, 2, 0, 4, 6, 123, 54, 2 };
            var received = new List<KafkaMessage>();


            topicConsumer.OnMessageReceived += (sender, message) =>
            {
                received.Add(message);
            };

            topicConsumer.Subscribe();
            QuixPartitionerDelegate partitioner = (topic, count, message) => 14;
            var topicProducer = client.GetRawTopicProducer(topicName,  partitioner);
            topicProducer.Publish(new KafkaMessage(null, toSend, null));
            var random = new Random();
            for (var ii = 0; ii < 100; ii++)
            {
                var key = new byte[64];
                random.NextBytes(key);
                topicProducer.Publish(new KafkaMessage(key, toSend, null));
            }


            SpinWait.SpinUntil(() => received.Count == 101, 5000);

            received.Count.Should().Be(101);
            received.All(y => y.TopicPartitionOffset.Partition.Value == 14).Should().BeTrue();


            topicConsumer.Dispose();
            topicProducer.Dispose();
        }
    }
}