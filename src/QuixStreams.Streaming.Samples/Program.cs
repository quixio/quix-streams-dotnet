using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuixStreams.Streaming.Samples.Samples;

namespace QuixStreams.Streaming.Model.Samples
{
    class Program
    {
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                if (cts.IsCancellationRequested) return;
                Console.WriteLine("Cancelling....");
                e.Cancel = true;
                cts.Cancel();
            };
            
            Logging.UpdateFactory(LogLevel.Debug);

           // ExampleReadWriteMessages(cts.Token);
           //ExampleReadWriteMessagesV2(cts.Token);
            
           // ExampleReadWriteWithManualCommitMessages(cts.Token);
           // ExampleReadWriteMessagesWithTimeout(cts.Token);

           ExampleReadWriteUsingQuixStreamingClient(cts.Token);
        }

        private static void ExampleReadWriteWithManualCommitMessages(in CancellationToken ctsToken)
        {
            new WriteSample().Start(ctsToken, Guid.NewGuid().ToString());
            new ReadSampleWithManualCommit().Start(ctsToken);
            try
            {
                Task.Delay(-1, ctsToken).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        private static void ExampleReadWriteMessages(in CancellationToken ctsToken)
        {
            var stream = Guid.NewGuid().ToString();
            new WriteSample().Start(ctsToken, stream);
            var read = new ReadSample();
            read.Start(stream);
            try
            {
                Task.Delay(-1, ctsToken).GetAwaiter().GetResult();
            }
            catch
            {
            }
            read.Stop();
        }
        
        private static void ExampleReadWriteMessagesV2(in CancellationToken ctsToken)
        {
            var stream = Guid.NewGuid().ToString();
            new WriteSample().Start(ctsToken, stream);
            var read = new ReadSampleV2();
            read.Start(stream);
            App.Run();
        }
        
        private static void ExampleReadWriteMessagesWithTimeout(in CancellationToken ctsToken)
        {
            var stream = Guid.NewGuid().ToString();
            new WriteSample().Start(ctsToken, stream);
            var read = new ReadSampleWithTimeout();
            read.Start(stream);
            try
            {
                Task.Delay(-1, ctsToken).GetAwaiter().GetResult();
            }
            catch
            {
            }
            read.Stop();
        }

        private static void ExampleReadWriteUsingQuixStreamingClient(in CancellationToken cancellationToken)
        {
            var quixStreamClient = new QuixStreamingClient(QuixStreams.Streaming.Samples.Configuration.QuixStreamingClientConfig.Token);
            quixStreamClient.ApiUrl = new Uri(QuixStreams.Streaming.Samples.Configuration.QuixStreamingClientConfig.PortalApi);

            using var topicConsumer = quixStreamClient.GetTopicConsumer("test-topic-sdk");
            using var topicProducer = quixStreamClient.GetTopicProducer("test-topic-sdk");

            var packageReceived = 0;
            topicConsumer.OnStreamReceived += (sender, consumer) =>
            {
                Console.WriteLine("Stream {0} received", consumer.StreamId);
                consumer.OnPackageReceived += (o, args) =>
                {
                    packageReceived++;
                };
            };
            topicConsumer.Subscribe();
            var stream = topicProducer.GetOrCreateStream("test-stream");
            stream.Timeseries.Buffer.AddTimestamp(DateTime.UtcNow).AddValue("parameter1", "somevalue").Publish();
            stream.Flush();
            stream.Close();
            SpinWait.SpinUntil(() => packageReceived > 0, TimeSpan.FromSeconds(5));
        }
    }
}


