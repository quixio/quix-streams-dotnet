using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using QuixStreams.Kafka;
using Xunit;

namespace QuixStreams.Transport.Kafka.Tests
{
    public class ConsumerLifecycleTests
    {
        private static ConsumeResult<byte[], byte[]> Record(bool eof = false) => new ConsumeResult<byte[], byte[]>
        {
            Topic = "test", Partition = 0, Offset = 0, IsPartitionEOF = eof,
            Message = eof ? null : new Message<byte[], byte[]> { Value = new byte[] { 1 } }
        };
        private static ConsumeException Fault() => new ConsumeException(Record(),
            new Error(ErrorCode.NotCoordinatorForGroup, "Failed to fetch committed offsets for 0 partition(s) in group \"secret\": Broker: Not coordinator"));
        private static TaskCompletionSource<T> Signal<T>() => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        private static KafkaConsumer Create(Func<IConsumer<byte[], byte[]>> factory, ConsumerRecoveryPolicy policy = null)
        {
            var config = new ConsumerConfiguration("unused", "group");
            if (policy != null) config.Recovery = policy;
            return new KafkaConsumer(config, new ConsumerTopicConfiguration("test", 0, Offset.Stored))
            { VerifyBrokerConnection = false, ConsumerFactory = _ => factory() };
        }
        private static IConsumer<byte[], byte[]> Native(Func<ConsumeResult<byte[], byte[]>> poll)
        {
            var native = Substitute.For<IConsumer<byte[], byte[]>>();
            native.Assignment.Returns(new List<TopicPartition> { new TopicPartition("test", 0) });
            native.Consume(Arg.Any<TimeSpan>()).Returns(_ => poll());
            return native;
        }
        private static Task<ConsumerDiagnosticsSnapshot> State(KafkaConsumer consumer, ConsumerLifecycleState state)
        {
            var signal = Signal<ConsumerDiagnosticsSnapshot>();
            consumer.DiagnosticsChanged += (_, value) => { if (value.State == state) signal.TrySetResult(value); };
            return signal.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Theory]
        [InlineData("Failed to fetch committed offsets for 0 partition(s)", true)]
        [InlineData("OffsetFetch failed", true)]
        [InlineData("Not coordinator during heartbeat", false)]
        [InlineData("MissingCodecException", false)]
        public void ClassificationRequiresCodeAndOperation(string reason, bool expected)
        {
            Assert.Equal(expected, KafkaConsumer.IsOffsetInitializationFailure(new Error(ErrorCode.NotCoordinatorForGroup, reason)));
            Assert.False(KafkaConsumer.IsOffsetInitializationFailure(new Error(ErrorCode.Local_Fail, reason)));
        }

        [Fact]
        public async Task OffsetFailureRecreatesOnceAndDeliversWithoutDisposingDuringPoll()
        {
            var first = Native(() => throw Fault());
            var second = Native(() => Record());
            var count = 0;
            using var consumer = Create(() => Interlocked.Increment(ref count) == 1 ? first : second);
            var delivered = Signal<bool>();
            consumer.OnMessageReceived = _ => { delivered.TrySetResult(true); return Task.CompletedTask; };
            var running = State(consumer, ConsumerLifecycleState.Running);
            consumer.Open();
            await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var value = await running;
            consumer.Close();
            Assert.Equal(2, count);
            Assert.Equal(2, value.Generation);
            Assert.Null(value.RecoveryEpisodeId);
            first.Received(1).Close(); first.Received(1).Dispose();
            second.Received(1).Dispose();
            Assert.DoesNotContain("secret", value.LastFailureReason);
        }

        [Fact]
        public async Task RepeatedFailuresKeepEpisodeAndEnforceBackoff()
        {
            var delays = new List<TimeSpan>();
            var values = new ConcurrentQueue<ConsumerDiagnosticsSnapshot>();
            var count = 0;
            using var consumer = Create(() => { count++; return Native(() => throw Fault()); });
            consumer.Delay = (delay, ct) => { delays.Add(delay); return Task.CompletedTask; };
            consumer.DiagnosticsChanged += (_, v) => values.Enqueue(v);
            var failed = State(consumer, ConsumerLifecycleState.Failed);
            consumer.Open();
            await failed;
            consumer.Close();
            Assert.Equal(6, count); // initial generation plus five replacement attempts
            Assert.Equal(new[] { 5d, 10d, 20d, 30d }, delays.Select(d => d.TotalSeconds));
            Assert.Single(values.Where(v => v.RecoveryEpisodeId.HasValue).Select(v => v.RecoveryEpisodeId).Distinct());
            Assert.Equal(5, values.Last(v => v.State == ConsumerLifecycleState.Failed).AttemptCount);
        }

        [Fact]
        public async Task EofInitializesWithoutDeliveringToApplication()
        {
            using var consumer = Create(() => Native(() => Record(true)));
            var delivered = 0;
            consumer.OnMessageReceived = _ => { Interlocked.Increment(ref delivered); return Task.CompletedTask; };
            var running = State(consumer, ConsumerLifecycleState.Running);
            consumer.Open();
            var value = await running;
            consumer.Close();
            Assert.Equal(0, delivered);
            Assert.NotNull(value.LastSuccessfulPollAt);
            Assert.Null(value.LastRecordAt);
        }

        [Fact]
        public async Task InitializationDeadlineRecoversAndNeverReportsRunning()
        {
            var now = DateTimeOffset.UtcNow;
            using var consumer = Create(() => Native(() => { now = now.AddSeconds(61); return null; }),
                new ConsumerRecoveryPolicy { MaximumAttempts = 1 });
            consumer.UtcNow = () => now;
            var states = new ConcurrentQueue<ConsumerLifecycleState>();
            consumer.DiagnosticsChanged += (_, v) => states.Enqueue(v.State);
            var failed = State(consumer, ConsumerLifecycleState.Failed);
            consumer.Open(); await failed; consumer.Close();
            Assert.DoesNotContain(ConsumerLifecycleState.Running, states);
            Assert.Contains(ConsumerLifecycleState.Recovering, states);
        }

        [Fact]
        public async Task UnexpectedExceptionFailsAndReleasesClose()
        {
            var native = Native(() => throw new InvalidOperationException("unexpected"));
            using var consumer = Create(() => native);
            var failed = State(consumer, ConsumerLifecycleState.Failed);
            consumer.Open(); await failed;
            await Task.Run(consumer.Close).WaitAsync(TimeSpan.FromSeconds(10));
            native.Received(1).Dispose();
        }

        [Fact]
        public async Task ThrowingDiagnosticSubscriberAndSynchronousCloseAreIsolated()
        {
            using var consumer = Create(() => Native(() => Record()));
            var closed = Signal<bool>();
            consumer.DiagnosticsChanged += (_, v) => throw new Exception("subscriber");
            consumer.DiagnosticsChanged += (_, v) =>
            {
                if (v.State != ConsumerLifecycleState.Running) return;
                consumer.Close(); closed.TrySetResult(true);
            };
            consumer.Open();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(ConsumerLifecycleState.Stopped, consumer.Snapshot.State);
        }

        [Fact]
        public async Task ShutdownTimeoutNeverDisposesConsumerInUse()
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var disposed = Signal<bool>();
            var native = Native(() => { entered.Set(); release.Wait(); return null; });
            native.When(n => n.Dispose()).Do(_ => disposed.TrySetResult(true));
            using var consumer = Create(() => native, new ConsumerRecoveryPolicy { ShutdownTimeout = TimeSpan.FromMilliseconds(20) });
            consumer.Open(); Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            try
            {
                consumer.Close();
                Assert.Equal(ConsumerLifecycleState.Failed, consumer.Snapshot.State);
                native.DidNotReceive().Dispose();
                consumer.Open(); // must not overlap the blocked owner
                native.Received(1).Assign(Arg.Any<IEnumerable<TopicPartitionOffset>>());
            }
            finally { release.Set(); }
            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task ErrorSubscriberCanDisposeDuringRecovery()
        {
            var native = Native(() => throw Fault());
            using var consumer = Create(() => native);
            var done = Signal<bool>();
            consumer.Delay = async (_, ct) => await Task.Delay(Timeout.Infinite, ct);
            consumer.OnErrorOccurred += (_, ex) => { consumer.Dispose(); done.TrySetResult(true); };
            consumer.Open(); await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(ConsumerLifecycleState.Stopped, consumer.Snapshot.State);
        }

        [Fact]
        public async Task RecoveryRequestedInsidePollProceedsAfterTimeout()
        {
            KafkaConsumer consumer = null;
            IConsumer<byte[], byte[]> first = null;
            first = Native(() => { consumer.ConsumerErrorHandler(first, Fault().Error); return null; });
            var count = 0;
            using (consumer = Create(() => ++count == 1 ? first : Native(() => Record(true))))
            {
                var running = State(consumer, ConsumerLifecycleState.Running);
                consumer.Open(); await running; consumer.Close();
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public async Task OtherCoordinatorErrorDoesNotRecreate()
        {
            var polls = 0; var creations = 0;
            using var consumer = Create(() =>
            {
                creations++;
                return Native(() => ++polls == 1
                    ? throw new ConsumeException(Record(), new Error(ErrorCode.NotCoordinatorForGroup, "heartbeat failed"))
                    : Record(true));
            });
            var running = State(consumer, ConsumerLifecycleState.Running);
            consumer.Open(); await running; consumer.Close();
            Assert.Equal(1, creations);
        }

        [Fact]
        public async Task ConfirmedEmptyAssignmentIsHealthyButSubscriptionAloneIsNot()
        {
            KafkaConsumer consumer = null;
            IConsumer<byte[], byte[]> native = null;
            var count = 0;
            native = Native(() =>
            {
                if (++count == 1)
                {
                    Assert.Equal(ConsumerLifecycleState.Starting, consumer.Snapshot.State);
                    consumer.PartitionsAssignedHandler(native, new List<TopicPartition>());
                }
                return null;
            });
            using (consumer = new KafkaConsumer(new ConsumerConfiguration("unused", "group"), new ConsumerTopicConfiguration("test"))
                { VerifyBrokerConnection = false, ConsumerFactory = _ => native })
            {
                var running = State(consumer, ConsumerLifecycleState.Running);
                consumer.Open(); var value = await running; consumer.Close();
                Assert.True(value.AssignmentSettled);
                Assert.Equal(0, value.AssignmentCount);
                native.Received(1).Subscribe(Arg.Any<IEnumerable<string>>());
            }
        }

        [Fact]
        public async Task StaleAssignmentCannotInitializeReplacement()
        {
            KafkaConsumer consumer = null;
            var first = Native(() => throw Fault());
            var count = 0; var now = DateTimeOffset.UtcNow;
            var second = Native(() =>
            {
                consumer.PartitionsAssignedHandler(first, new List<TopicPartition>());
                now = now.AddSeconds(61);
                return null;
            });
            using (consumer = Create(() => ++count == 1 ? first : second, new ConsumerRecoveryPolicy { MaximumAttempts = 1 }))
            {
                consumer.UtcNow = () => now;
                var failed = State(consumer, ConsumerLifecycleState.Failed);
                consumer.Open(); var value = await failed; consumer.Close();
                Assert.Equal(2, value.Generation);
                Assert.Equal(1, value.AssignmentCount);
                Assert.Null(value.LastRecordAt);
            }
        }

        [Fact]
        public async Task ElapsedBudgetStopsAttemptsDuringBackoff()
        {
            var now = DateTimeOffset.UtcNow;
            var delays = new List<double>(); var creations = 0;
            using var consumer = Create(() => { creations++; return Native(() => throw Fault()); },
                new ConsumerRecoveryPolicy { EpisodeTimeout = TimeSpan.FromSeconds(12) });
            consumer.UtcNow = () => now;
            consumer.Delay = (delay, ct) => { delays.Add(delay.TotalSeconds); now += delay; return Task.CompletedTask; };
            var failed = State(consumer, ConsumerLifecycleState.Failed);
            consumer.Open(); await failed; consumer.Close();
            Assert.Equal(new[] { 5d, 7d }, delays);
            Assert.Equal(3, creations);
        }

        [Fact]
        public async Task FatalErrorIsTerminalUntilExplicitOpen()
        {
            var creations = 0;
            using var consumer = Create(() => ++creations == 1
                ? Native(() => throw new ConsumeException(Record(), new Error(ErrorCode.Local_Fatal, "fatal", true)))
                : Native(() => Record(true)));
            var failed = State(consumer, ConsumerLifecycleState.Failed);
            consumer.Open(); await failed;
            consumer.Close();
            Assert.Equal(1, creations);
            var running = State(consumer, ConsumerLifecycleState.Running);
            consumer.Open(); await running; consumer.Close();
            Assert.Equal(2, creations);
        }

        [Fact]
        public async Task ShutdownDuringConstructionDoesNotSubscribeReplacement()
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var disposed = Signal<bool>();
            var replacement = Native(() => Record());
            replacement.When(n => n.Dispose()).Do(_ => disposed.TrySetResult(true));
            var count = 0;
            using var consumer = new KafkaConsumer(new ConsumerConfiguration("unused", "group")
                { Recovery = new ConsumerRecoveryPolicy { ShutdownTimeout = TimeSpan.FromMilliseconds(20) } },
                new ConsumerTopicConfiguration("test")) { VerifyBrokerConnection = false };
            consumer.ConsumerFactory = _ =>
            {
                if (++count == 1) return Native(() => throw Fault());
                entered.Set(); release.Wait(); return replacement;
            };
            consumer.Open(); Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            try { consumer.Close(); Assert.Equal(ConsumerLifecycleState.Failed, consumer.Snapshot.State); }
            finally { release.Set(); }
            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            consumer.Close();
            replacement.DidNotReceive().Subscribe(Arg.Any<IEnumerable<string>>());
            replacement.DidNotReceive().Consume(Arg.Any<TimeSpan>());
            replacement.Received(1).Dispose();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void InvalidAttemptBoundRejected(int attempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(() => null,
                new ConsumerRecoveryPolicy { MaximumAttempts = attempts }));
        }
    }
}
