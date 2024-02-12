using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuixStreams.Kafka.Transport.SerDes;
using QuixStreams.Streaming;
using QuixStreams.Streaming.Models;
using QuixStreams.Streaming.Utils;
using QuixStreams.Telemetry.Kafka;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Tester
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
            
            
            CodecSettings.SetGlobalCodecType(CodecType.Json);
            PackageSerializationSettings.Mode = PackageSerializationMode.Header; // required for new mode
            
            var client = new KafkaStreamingClient(Configuration.Config.BrokerList, Configuration.Config.Security);


            if (Configuration.Mode == ClientRunMode.Producer)
            {
                Produce(client, cts.Token);
                return;
            }

            Consume(client, cts.Token);
        }

        private static void Produce(KafkaStreamingClient client, CancellationToken cancellationToken)
        {
            using var topicProducer = client.GetTopicProducer(Configuration.Config.Topic);

            var streams = new IStreamProducer[Configuration.ProducerConfig.NumberOfStreams];
            for (int ii = 0; ii < streams.Length; ii++)
            {
                var stream = topicProducer.CreateStream();
                GenerateDefinition(stream);
                streams[ii] = stream;

            }

            if (Configuration.ProducerConfig.TimeseriesEnabled)
            {
                Console.WriteLine("Will produce Timeseries data");
                Task.Run(() => GenerateTimeseriesData(streams, cancellationToken));
            }
            
            if (Configuration.ProducerConfig.EventsEnabled)
            {
                Console.WriteLine("Will produce Event data");
                Task.Run(() => GenerateEventData(streams, cancellationToken));
            }

            try
            {
                cancellationToken.WaitHandle.WaitOne();
            }
            catch
            {

            }
            finally
            {
                Console.WriteLine("Finished producing");
            }
        }

        private static void GenerateDefinition(IStreamProducer stream)
        {
            if (Configuration.ProducerConfig.ParameterDefinitionCount > 0)
            {
                var names = new string[]
                {
                    "numeric_parameter_INDEX",
                    "string_parameter_INDEX",
                    "binary_parameter_INDEX"
                };
                
                var locations = new string[]
                {
                    "/some/path",
                    "/some/other/path",
                    "/",
                    "/yet/another/path"
                };
                
                var customProp = new string[]
                {
                    "{}",
                    "{\"my\":\"json\"}",
                    "abracadabra"
                };
                
                for (var i = 0; i <= Configuration.ProducerConfig.ParameterDefinitionCount; i++)
                {
                    var name = names[i % names.Length].Replace("INDEX", i.ToString());
                    var location = locations[i % locations.Length];
                    var customPRop = locations[i % locations.Length];
                    stream.Timeseries.AddLocation(location)
                        .AddDefinition(name, description: "parameter with some description")
                        .SetCustomProperties(customPRop);
                }
            }

            if (Configuration.ProducerConfig.EventDefinitionCount > 0)
            {
                var names = new string[]
                {
                    "event_one_INDEX",
                    "another_INDEX",
                    "fun_event_INDEX"
                };
                
                var locations = new string[]
                {
                    "/some/path",
                    "/some/other/path",
                    "/",
                    "/yet/another/path"
                };
                
                for (var i = 0; i <= Configuration.ProducerConfig.EventDefinitionCount; i++)
                {
                    var name = names[i % names.Length].Replace("INDEX", i.ToString());
                    var location = locations[i % locations.Length];
                    stream.Timeseries.AddLocation(location)
                        .AddDefinition(name, description: "parameter with some description");
                }
            }
        }

        private static void GenerateEventData(IStreamProducer[] streams, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var counter = 0;
            var sleep = (int)(counter / Configuration.ProducerConfig.EventRate);
            if (sleep < 21) sleep = 21;
            var expectedCounter = 0;
            var printAfter = start.Add(TimeSpan.FromSeconds(1));
            
            var streamIndex = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                
                var now = DateTime.UtcNow;
                var elapsed = (now - start).TotalSeconds;
                expectedCounter = (int)Math.Ceiling(elapsed * Configuration.ProducerConfig.EventRate);
                while (counter < expectedCounter && !cancellationToken.IsCancellationRequested)
                {
                    var stream = streams[streamIndex % streams.Length];
                    streamIndex++;
                    var random = new Random();
                    var obj = new
                    {
                        PropOne = "Value " + random.Next(10, 99999),
                        PropTwo = random.NextDouble() * 10000,
                        PropThree = random.Next(0, 2) == 1
                    };

                    var builder = stream.Events.AddTimestamp(DateTime.UtcNow);

                    builder.AddValue("an_event", Newtonsoft.Json.JsonConvert.SerializeObject(obj));

                    if (random.Next(0, 2) == 1) builder.AddTag("Random_Tag", $"tag{random.Next(0, 10)}");
                    builder.Publish();
                    counter++;

                    if (printAfter < DateTime.UtcNow)
                    {
                        printAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(1));
                        Console.WriteLine($"Sent {counter} event messages, expected {expectedCounter}");
                    }
                }

                Thread.Sleep(sleep);
                
            }
        }

        private static void GenerateTimeseriesData(IStreamProducer[] streams, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var counter = 0;
            var sleep = (int)(counter / Configuration.ProducerConfig.TimeseriesRate);
            if (sleep < 21) sleep = 21;
            var expectedCounter = 0;
            var printAfter = start.Add(TimeSpan.FromSeconds(1));

            var streamIndex = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - start).TotalSeconds;
                expectedCounter = (int)Math.Ceiling(elapsed * Configuration.ProducerConfig.TimeseriesRate);
                while (counter < expectedCounter && !cancellationToken.IsCancellationRequested)
                {
                    var stream = streams[streamIndex % streams.Length];
                    streamIndex++;
                    var tsd = new TimeseriesData(Configuration.ProducerConfig.RowPerTimeseries);
                    for (var ii = 0; ii < Configuration.ProducerConfig.RowPerTimeseries; ii++)
                    {
                        var expectedTime = start + TimeSpan.FromSeconds(counter).Add(TimeSpan.FromMilliseconds(ii));
                        var tsdb = tsd.AddTimestamp(expectedTime);

                        var random = new Random();
                        for (var jj = 1; jj <= Configuration.ProducerConfig.TimeseriesParameterCount; jj++)
                        {
                            if (random.Next(0, 3) == 1)
                            {
                                tsdb.AddValue($"numeric_parameter_{jj}", jj);
                                continue;
                            }

                            if (random.Next(0, 3) == 1)
                            {
                                tsdb.AddValue($"string_parameter_{jj}", $"value_{jj}");
                                continue;
                            }

                            if (random.Next(0, 3) == 1)
                            {
                                tsdb.AddValue($"binary_parameter_{jj}", Encoding.UTF8.GetBytes($"binary_value_{jj}"));
                                continue;
                            }
                        }

                        if (random.Next(0, 2) == 1) tsdb.AddTag("Random_Tag", $"tag{random.Next(0, 10)}");
                    }

                    stream.Timeseries.Publish(tsd);

                    counter++;

                    if (printAfter < DateTime.UtcNow)
                    {
                        printAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(1));
                        Console.WriteLine($"Sent {counter} timeseries messages, expected {expectedCounter}");
                    }
                }
                Thread.Sleep(sleep);
            }
        }
        
        private static void Consume(KafkaStreamingClient client, CancellationToken cancellationToken)
        {
            using var topicConsumer = client.GetTopicConsumer(Configuration.Config.Topic, Configuration.Config.ConsumerGroup, CommitMode.Automatic, AutoOffsetReset.Earliest);

            long totalTimeSeriesMessagesRead = 0;
            long totalEventMessagesRead = 0;
            long totalStreamsRead = 0;

            topicConsumer.OnStreamReceived += (sender, consumer) =>
            {
                Interlocked.Increment(ref totalStreamsRead);

                if (Configuration.ConsumerConfig.PrintStreams)
                {
                    Console.WriteLine($"Received new stream {consumer.StreamId}");
                }

                consumer.Timeseries.OnRawReceived += (o, args) =>
                {
                    Interlocked.Increment(ref totalTimeSeriesMessagesRead);
                    if (Configuration.ConsumerConfig.PrintTimeseries)
                    {
                        Console.WriteLine($"Received new timeseries data for {consumer.StreamId}");
                        var asJson = Newtonsoft.Json.JsonConvert.SerializeObject(args.Data, Formatting.Indented);
                        Console.WriteLine(asJson);
                    }
                };

                consumer.Events.OnDataReceived += (o, args) =>
                {
                    Interlocked.Increment(ref totalEventMessagesRead);
                    if (Configuration.ConsumerConfig.PrintEvents)
                    {
                        Console.WriteLine($"Received new event data for {consumer.StreamId}");
                        var asJson = Newtonsoft.Json.JsonConvert.SerializeObject(args.Data, Formatting.Indented);
                        Console.WriteLine(asJson);
                    }
                };
            };

            
            if (Configuration.ConsumerConfig.PrintAverage)
            {
                Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Read {0} streams, {1} timestamps, {2} events", totalStreamsRead, totalTimeSeriesMessagesRead, totalEventMessagesRead);
                        await Task.Delay(1000);
                    }
                });
            }

            
            topicConsumer.Subscribe();

            
            try
            {
                cancellationToken.WaitHandle.WaitOne();
            }
            catch
            {
                
            }
        }
    }
}


