using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using QuixStreams.Kafka;
using Xunit;

namespace QuixStreams.Transport.Kafka.Tests
{
    [Collection("Kafka Container Collection")]
    public class ConsumerRecoveryIntegrationTests
    {
        private readonly KafkaDockerTestFixture fixture;
        public ConsumerRecoveryIntegrationTests(KafkaDockerTestFixture fixture) { this.fixture = fixture; }

        [Fact]
        public async Task RecoveryPreservesCommittedBoundary()
        {
            var topic = "recovery-" + Guid.NewGuid().ToString("N");
            var group = "recovery-" + Guid.NewGuid().ToString("N");
            await fixture.EnsureTopic(topic, 1);
            using (var producer = new ProducerBuilder<byte[], byte[]>(new ProducerConfig { BootstrapServers = fixture.BrokerList }).Build())
                for (var i = 0; i < 10; i++)
                    await producer.ProduceAsync(topic, new Message<byte[], byte[]> { Value = new[] { (byte)i } });

            using var consumer = new KafkaConsumer(new ConsumerConfiguration(fixture.BrokerList, group)
                { AutoOffsetReset = AutoOffsetReset.Earliest }, new ConsumerTopicConfiguration(topic));
            IConsumer<byte[], byte[]> native = null;
            var creations = 0;
            var error = new Error(ErrorCode.NotCoordinatorForGroup, "Failed to fetch committed offsets for 0 partition(s)");
            consumer.ConsumerFactory = builder =>
            {
                native = builder.Build();
                // Wrapper fault injection only: this does not reproduce the native protocol defect.
                if (++creations == 2) consumer.ConsumerErrorHandler(native, error);
                return native;
            };
            var remaining = new List<long>();
            var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            consumer.OnMessageReceived = message =>
            {
                var offset = message.TopicPartitionOffset;
                if (creations == 1 && offset.Offset.Value == 3)
                {
                    consumer.Commit(new[] { offset }); // public API commits next offset 4
                    consumer.ConsumerErrorHandler(native, error);
                }
                if (creations > 1)
                {
                    remaining.Add(offset.Offset.Value);
                    if (offset.Offset.Value == 9) finished.TrySetResult(true);
                }
                return Task.CompletedTask;
            };
            consumer.Open();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(90));
            consumer.Close();
            Assert.Equal(3, creations);
            Assert.Equal(Enumerable.Range(4, 6).Select(i => (long)i), remaining);
            using var verifier = new ConsumerBuilder<byte[], byte[]>(new ConsumerConfig
                { BootstrapServers = fixture.BrokerList, GroupId = group, EnableAutoCommit = false }).Build();
            Assert.Equal(4, verifier.Committed(new[] { new TopicPartition(topic, 0) }, TimeSpan.FromSeconds(10)).Single().Offset.Value);
        }

        [Fact]
        public async Task EmptyTopicAndExcessGroupMemberBecomeHealthy()
        {
            var topic = "standby-" + Guid.NewGuid().ToString("N");
            var group = "standby-" + Guid.NewGuid().ToString("N");
            await fixture.EnsureTopic(topic, 1);
            KafkaConsumer Member() => new KafkaConsumer(new ConsumerConfiguration(fixture.BrokerList, group,
                new Dictionary<string, string> { ["partition.assignment.strategy"] = "range" }), new ConsumerTopicConfiguration(topic))
                { VerifyBrokerConnection = false };
            using var first = Member(); using var second = Member();
            var standby = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var assigned = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Changed(object sender, ConsumerDiagnosticsSnapshot value)
            {
                if (value.State != ConsumerLifecycleState.Running) return;
                if (value.AssignmentCount == 0 && value.AssignmentSettled) standby.TrySetResult(true);
                if (value.AssignmentCount == 1 && value.LastSuccessfulPollAt.HasValue && !value.LastRecordAt.HasValue) assigned.TrySetResult(true);
            }
            first.DiagnosticsChanged += Changed; second.DiagnosticsChanged += Changed;
            first.Open(); second.Open();
            await Task.WhenAll(standby.Task, assigned.Task).WaitAsync(TimeSpan.FromSeconds(90));
        }
    }
}
