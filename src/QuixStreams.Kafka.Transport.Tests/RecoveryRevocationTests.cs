using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using QuixStreams.Kafka.Transport.SerDes;
using QuixStreams.Kafka.Transport.Tests.Helpers;
using Xunit;

namespace QuixStreams.Kafka.Transport.Tests
{
    public class RecoveryRevocationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RevocationDiscardsFragmentsBeforeReplayAndCommitsOnlyCompletedPackages(bool autoCommit)
        {
            PackageSerializationSettings.Mode = PackageSerializationMode.Header;
            var native = Substitute.For<IKafkaConsumer>();
            using var transport = new KafkaTransportConsumer(native, options =>
            {
                options.CommitOptions.AutoCommitEnabled = autoCommit;
                options.CommitOptions.CommitEvery = 1;
                options.CommitOptions.CommitInterval = null;
            });
            var data = new byte[2000];
            new Random(42).NextBytes(data);
            var message = ModelFactory.CreateKafkaMessage("key", new { Data = Convert.ToBase64String(data) });
            var fragments = new KafkaMessageSplitter(message.HeaderSize + KafkaMessageSplitter.ExpectedHeaderSplitInfoSize + 512).Split(message).Select((fragment, i) =>
                new TestProducedKafkaMessage(fragment, new TopicPartitionOffset("test", 0, i))).ToList();
            Assert.True(fragments.Count > 1);
            var delivered = 0;
            transport.OnPackageReceived = _ => { delivered++; return Task.CompletedTask; };
            await native.OnMessageReceived(fragments[0]);
            native.OnRevoked += Raise.EventWith(new RevokedEventArgs(new[] { new TopicPartitionOffset("test", 0, Offset.Unset) }));
            foreach (var fragment in fragments.Skip(1)) await native.OnMessageReceived(fragment);
            Assert.Equal(0, delivered); // old first fragment must not complete with replacement fragments
            native.DidNotReceive().Commit(Arg.Any<ICollection<TopicPartitionOffset>>());
            foreach (var fragment in fragments) await native.OnMessageReceived(fragment);
            Assert.Equal(1, delivered);
            native.Received(autoCommit ? 1 : 0).Commit(Arg.Is<ICollection<TopicPartitionOffset>>(offsets =>
                offsets.Single().Offset == fragments.First().TopicPartitionOffset.Offset));
        }

        [Fact]
        public async Task RevocationClearsProcessedButUncommittedOffsetsFromPriorGeneration()
        {
            var native = Substitute.For<IKafkaConsumer>();
            using var transport = new KafkaTransportConsumer(native, options =>
            {
                options.CommitOptions.CommitEvery = 2;
                options.CommitOptions.CommitInterval = null;
            });
            var message = ModelFactory.CreateKafkaMessage("key", new { Value = 1 });
            await native.OnMessageReceived(new TestProducedKafkaMessage(message, new TopicPartitionOffset("test", 0, 100)));
            native.OnRevoked += Raise.EventWith(new RevokedEventArgs(new[] { new TopicPartitionOffset("test", 0, Offset.Unset) }));
            await native.OnMessageReceived(new TestProducedKafkaMessage(message, new TopicPartitionOffset("test", 0, 50)));
            native.DidNotReceive().Commit(Arg.Any<ICollection<TopicPartitionOffset>>());
            await native.OnMessageReceived(new TestProducedKafkaMessage(message, new TopicPartitionOffset("test", 0, 51)));
            native.Received(1).Commit(Arg.Is<ICollection<TopicPartitionOffset>>(offsets => offsets.All(p => p.Offset.Value < 100)));
        }
    }
}
