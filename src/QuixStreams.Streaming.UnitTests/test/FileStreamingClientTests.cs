using System;
using System.Threading;
using Quix.TestBase.Extensions;
using QuixStreams.Streaming.TestHelpers;
using QuixStreams.Telemetry.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace QuixStreams.Streaming.UnitTests.test
{
    public class FileStreamingClientTests
    {
        private readonly ITestOutputHelper output;

        public FileStreamingClientTests(ITestOutputHelper output)
        {
            this.output = output;
            Logging.Factory = output.CreateLoggerFactory(); // FOR DEBUGGING!
        }
        
        [Fact]
        public void FileStreamingClient_ShouldWriteAndReadFromFile()
        {
            var client = new FileStreamingClient();

            var producer = client.GetFileProducer("test");
            var consumer = client.GetFileConsumer("test", AutoOffsetReset.Earliest);
            
            consumer.OnStreamReceived += (sender, streamConsumer) =>
            {
                this.output.WriteLine($"Received {streamConsumer.StreamId}");

                streamConsumer.Timeseries.OnDataReceived += (o, args) =>
                {
                    this.output.WriteLine($"Received data for {streamConsumer.StreamId}");
                };
            };
            consumer.Subscribe();


            var outputStream = producer.GetOrCreateStream("mystreamid2");

            for (var ii = 0; ii <= 20; ii++)
            {
                outputStream.Timeseries.Buffer
                    .AddTimestamp(DateTime.UtcNow)
                    .AddValue("SomeValue", 123)
                    .Publish();
                
                Thread.Sleep(100);
            }

            outputStream.Flush();
            
            
            Thread.Sleep(5000);
            
        }
    }
}