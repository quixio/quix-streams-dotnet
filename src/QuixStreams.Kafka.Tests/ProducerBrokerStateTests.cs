using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuixStreams.Kafka;
using Xunit;

namespace QuixStreams.Transport.Kafka.Tests
{
    [CollectionDefinition("Producer broker state logging", DisableParallelization = true)]
    public class ProducerBrokerStateLoggingCollection { }

    [Collection("Producer broker state logging")]
    public class ProducerBrokerStateTests
    {
        [Fact]
        public async Task PublishUsesStableBrokerSnapshotWhileNativeCallbackRenamesBroker()
        {
            var native = Substitute.For<IProducer<byte[], byte[]>>();
            native.When(p => p.Produce(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(),
                Arg.Any<Action<DeliveryReport<byte[], byte[]>>>())).Do(call =>
                call.ArgAt<Action<DeliveryReport<byte[], byte[]>>>(2)(new DeliveryReport<byte[], byte[]>
                {
                    Topic = "test", Partition = 0, Offset = 0, Error = new Error(ErrorCode.NoError)
                }));
            var logger = new CallbackLogger();
            var factory = Substitute.For<ILoggerFactory>();
            factory.CreateLogger(Arg.Any<string>()).Returns(logger);
            var previousFactory = Logging.Factory;
            KafkaProducer producer;
            try
            {
                Logging.Factory = factory;
                producer = new KafkaProducer(new ProducerConfiguration("unused"), new ProducerTopicConfiguration("test"),
                    _ => native);
            }
            finally { Logging.Factory = previousFactory; }

            using (producer)
            {
                producer.ProducerLogHandler(native, State("first", "DOWN"));
                producer.ProducerLogHandler(native, State("second", "DOWN"));
                producer.ErrorHandler(native, new Error(ErrorCode.Local_AllBrokersDown, "2/2 brokers are down"));
                var updates = 0;
                logger.OnLog = message =>
                {
                    if (!message.Contains("has state DOWN") || updates != 0) return;
                    updates++;
                    // The send-side enumeration has begun. A native callback must be able to
                    // update it from another thread before enumeration resumes, without taking sendLock.
                    Task.Run(() =>
                    {
                        producer.ProducerLogHandler(native, new LogMessage("test", SyslogLevel.Debug, "UPDATE",
                            "Name changed from first to renamed"));
                        producer.ProducerLogHandler(native, State("third", "UP"));
                    }).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                };

                await producer.Publish(new KafkaMessage(null, new byte[] { 1 })).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(1, updates);
                var states = logger.Messages.Where(m => m.Contains("has state")).ToArray();
                Assert.Equal(2, states.Length);
                Assert.Contains(states, m => m.Contains("Broker first has state DOWN"));
                Assert.Contains(states, m => m.Contains("Broker second has state DOWN"));

                // The next check must see the callback's updates, with the old broker name removed.
                producer.ErrorHandler(native, new Error(ErrorCode.Local_AllBrokersDown, "3/3 brokers are down"));
                await producer.Publish(new KafkaMessage(null, new byte[] { 2 })).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Contains(logger.Messages, m => m.Contains("At least 1/3 brokers are up"));
                native.Received(2).Produce(Arg.Any<string>(), Arg.Any<Message<byte[], byte[]>>(),
                    Arg.Any<Action<DeliveryReport<byte[], byte[]>>>());
            }
        }

        private static LogMessage State(string broker, string state) => new LogMessage("test", SyslogLevel.Debug,
            "STATE", $"[thrd:test]: {broker}: Broker changed state INIT -> {state}");

        private sealed class CallbackLogger : ILogger
        {
            public ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();
            public Action<string> OnLog { get; set; }
            public bool IsEnabled(LogLevel logLevel) => true;
            public IDisposable BeginScope<TState>(TState state) => null;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                Messages.Enqueue(message);
                OnLog?.Invoke(message);
            }
        }
    }
}
