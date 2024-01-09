![Quix - React to data, fast](https://github.com/quixio/quix-streams-dotnet/blob/main/images/quixstreams-banner.jpg)

[//]: <> (This will be a banner image w/ the name e.g. Quix Streams.)

[![Quix on Twitter](https://img.shields.io/twitter/url?label=Twitter&style=social&url=https%3A%2F%2Ftwitter.com%2Fquix_io)](https://twitter.com/quix_io)
[![The Stream Community Slack](https://img.shields.io/badge/-The%20Stream%20Slack-blueviolet)](https://quix.io/slack-invite)
[![Linkedin](https://img.shields.io/badge/LinkedIn-0A66C2.svg?logo=linkedin)](https://www.linkedin.com/company/70925173/)
[![Events](https://img.shields.io/badge/-Events-blueviolet)](https://quix.io/community#events)
[![YouTube](https://img.shields.io/badge/YouTube-FF0000.svg?logo=youtube)](https://www.youtube.com/channel/UCrijXvbQg67m9-le28c7rPA)
[![Docs](https://img.shields.io/badge/-Docs-blueviolet)](https://www.quix.io/docs/client-library-intro.html)
[![Roadmap](https://img.shields.io/badge/-Roadmap-red)](https://github.com/orgs/quixio/projects/1)

# Quix Streams

Quix Streams is a cloud-native library for processing data in Kafka. It’s designed to give you the power of a distributed system in a lightweight library by combining the low-level scalability and resiliency features of Kafka and Kubernetes in a highly abstracted and easy to use interface.

Quix Streams has the following benefits:

 - No orchestrator, no server-side engine.

 - Simplified [state management](https://quix.io/docs/client-library/state-management.html) backed by Kubernetes PVC for enhanced resiliency.

 - Resilient horizontal scaling using [Streaming Context](https://quix.io/docs/client-library/features/streaming-context.html).

 - Native support for structured and semistructured (time-series) and unstructured (binary) data files.

 - Support for handling larger data files (video, audio etc) in Kafka with enhanced serialisation and deserialisation.

 - Treats time as a first class citizen - time being the most important factor in real-TIME applications!


Use Quix Streams if you’re building machine learning/AI and physics-based applications that depend on real-time data from Kafka to deliver quick, reliable insights and efficient end-user experiences. 


## Getting started 🏄

### Install Quix Streams

Install Quix streams using [nuget](https://www.nuget.org/packages/QuixStreams.Streaming).

### Install Kafka

This library needs to utilize a message broker to send and receive data. Quix uses [Apache Kafka](https://kafka.apache.org/) because it is the leading message broker in the field of streaming data, with enough performance to support high volumes of time-series data, with minimum latency.

**To install and test Kafka locally**:
* Download the Apache Kafka binary from the [Apache Kafka Download](https://kafka.apache.org/downloads) page.
* Extract the contents of the file to a convenient location (i.e. `kafka_dir`), and start the Kafka services with the following commands:<br><br>

  * **Linux / macOS**
    ```
    <kafka_dir>/bin/zookeeper-server-start.sh config/zookeeper.properties
    <kafka_dir>/bin/zookeeper-server-start.sh config/server.properties
    ```

  * **Windows**
    ```
    <kafka_dir>\bin\windows\zookeeper-server-start.bat.\config\zookeeper.properties
    <kafka_dir>\bin\windows\kafka-server-start.bat .\config\server.properties
    ```
* Create a test topic with the `kafka-topics` script.
  
  * **Linux / macOS**
    `<kafka_dir>/bin/kafka-topics.sh --create --topic mytesttopic --bootstrap-server localhost:9092`

  * **Windows**
    `bin\windows\kafka-topics.bat --create --topic mytesttopic --bootstrap-server localhost:9092`

You can find more detailed instructions in Apache Kafka's [official documentation](https://kafka.apache.org/quickstart).

To get started with Quix Streams, we recommend following the comprehensive [Quick Start guide](https://quix.io/docs/client-library/quickstart.html) in our official documentation. 

However, the following examples will give you a basic idea of how to produce and consume data with Quix Streams.:

### Producing time-series data

Here's an example of how to <b>produce</b> time-series data into a Kafka Topic.

``` csharp
// Open the topic producer which will be used to send data to a topic
using var topicProducer = client.GetTopicProducer("mytesttopic");

// Set stream ID or leave parameters empty to get stream ID generated.
var stream = topicProducer.CreateStream();
stream.Properties.Name = "Hello World stream";

// Add metadata about time series data you are about to send. 
stream.Timeseries.AddDefinition("ParameterA").SetRange(-1.2, 1.2);
stream.Timeseries.Buffer.TimeSpanInMilliseconds = 100;

Console.WriteLine("Sending values for 30 seconds.");

for (var index = 0; index < 3000; index++)
{
    stream.Timeseries
        .Buffer
        .AddTimestamp(DateTime.UtcNow)
        .AddValue("ParameterA", Math.Sin(index / 100.0) + Math.Sin(index) / 5.0)
        .Publish();
    
    Thread.Sleep(10);
}

Console.WriteLine("Closing stream");
stream.Close();
```

### Consuming time-series data

Here's an example of how to <b>consume</b> time-series data from a Kafka Topic:

``` csharp
// Connect to your kafka client
var client = new KafkaStreamingClient("127.0.0.1:9092");

// get the topic consumer for a specific consumer group
var topicConsumer = client.GetTopicConsumer("TestTopic", "myConsumer", autoOffset: AutoOffsetReset.Latest);

// subscribe to new streams received
topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    // subscribe to incoming timeseries
    consumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            // Example read of a numeric value
            var rpm = timestamp.Parameters["EngineRPM"].NumericValue;
        }
    };
};

Console.WriteLine("Listening to streams. Press CTRL-C to exit.");

// Handle termination signals and provide a graceful exit
App.Run();
```

Quix Streams allows multiple configurations to leverage resources while consuming and producing data from a Topic depending on the use case, frequency, language, and data types. 

For full documentation of how to [<b>consume</b>](https://www.quix.io/docs/client-library/subscribe.html) and [<b>produce</b>](https://www.quix.io/docs/client-library/publish.html) time-series and event data with Quix Streams, [visit our docs](https://www.quix.io/docs/client-library-intro.html).

## Library features

The following features are designed to address common issues faced when developing real-time streaming applications:

### Streaming contexts
Streaming contexts allow you to bundle data from one data source into the same scope with supplementary metadata—thus enabling workloads to be horizontally scaled with multiple replicas.

* In the following sample, the `CreateStream` function is used to create a stream called _bus-123AAAV_ which gets assigned to one particular consumer and will receive messages in the correct order: 

``` csharp
var topicProducer = client.GetTopicProducer("data");
var stream = topicProducer.CreateStream("bus-123AAAV");

// Message 1 sent (the stream context)
stream.Properties.Name = "BUS 123 AAAV";
// Message 2 sent (the human-readable identifier the bus)
stream.Timeseries
    .Buffer
    .AddTimestamp(DateTime.UtcNow)
    .AddValue("Lat", 1.23)
    .AddValue("Long", 4.56)
    .Publish();
// Message 3 sent (the time-series telemetry data from the bus)

stream.Events
    .AddTimestampNanoseconds(DateTime.UtcNow.ToUnixNanoseconds())
    .AddValue("driver_bell", "Doors 3 bell activated by passenger")
    .Publish();
// Message 4 sent (an event related to something that happened on the bus)
```

### Time-series data serialization and deserialization

Quix Streams serializes and deserializes time-series data using different codecs and optimizations to <b>minimize payloads</b> in order to increase throughput and reduce latency.

* The following example shows data being appended to as stream with the `add_value` method.<br><br>

``` csharp
// Open the producer topic where the data should be published.
var topicProducer = client.GetTopicProducer("data");
// Create a new stream for each device.
var stream = topicProducer.CreateStream("bus-123AAAV");
Console.WriteLine("Sending values for 30 seconds.")

for (var index = 0; index < 30; index++)
{
    stream.Timeseries
        .Buffer
        .AddTimestamp(DateTime.UtcNow)
        .AddValue("Lat", Math.Sin(index / 100.0) + Math.Sin(index) / 5.0)
        .AddValue("Long", Math.Sin(index / 200.0) + Math.Sin(index) / 5.0)
        .Publish();
}
```

### Built-in time-series buffers

If you’re sending data at <b>high frequency</b>, processing each message can be costly. Alternatively your business logic may be best executed using a certain volume of data. The library provides built-in time-series buffers for producing and consuming, allowing several configurations for balancing between latency and cost.

* For example, you can configure the library to release values from the buffer whenever 100 timestamps are collected or when a certain number of milliseconds in data have elapsed (note that this is using time in the data, not the consumer clock).

``` csharp
// subscribe to new streams received
topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    // create buffer
    var buffer = consumer.Timeseries.CreateBuffer(new TimeseriesBufferConfiguration()
    {
        PacketSize = 100,
        TimeSpanInMilliseconds = 100
    });
    // subscribe to incoming timeseries
    buffer.OnDataReleased += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            // Example read of a numeric value
            var rpm = timestamp.Parameters["EngineRPM"].NumericValue;
        }
    };
};
```


For a detailed overview of built-in buffers, [visit our documentation](https://quix.io/docs/client-library/features/builtin-buffers.html).

### Multiple data types

This library allows you to produce and consume different types of mixed data in the same timestamp, like <b>Numbers</b>, <b>Strings</b> or <b>Binary data</b>.

* For example, you can produce both time-series data and large binary blobs together.<br><br>

    Often, you’ll want to combine time series data with binary data. In the following example, we combine bus's onboard camera with telemetry from its ECU unit so we can analyze the onboard camera feed with context.

    ``` csharp 
    // Open the producer topic where to publish data.
    using var topicProducer = client.GetTopicProducer("mytesttopic");

    // Create new stream for each device
    var stream = topicProducer.CreateStream("bus-123AAAV");

    stream.Timeseries.Buffer
        .AddTimestamp(DateTime.UtcNow)
        .AddValue("numeric", 123.432)
        .AddValue("string", "green")
        .AddValue("binary", Encoding.UTF8.GetBytes("binary"));
    ```

* You can also produce events that include payloads:<br><br>For example, you might need to listen for changes in time-series or binary streams and produce an event (such as "speed limit exceeded"). These  might require some kind of document to send along with the event message (e.g. transaction invoices, or a speeding ticket with photographic proof). Here's an example for a speeding camera:
  
``` csharp
consumer.OnStreamReceived += (sender, streamConsumer) =>
{
    streamConsumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            var speed = timestamp.Parameters["speed"].NumericValue;
            if (speed > 130)
            {
                // create a document that will be consumed by the ticket service.
                var ticket = new
                {
                    speed = speed,
                    fine = (speed - 130) * 100,
                    photo_proof = timestamp.Parameters["camera_frame"].BinaryValue
                };

                producer.GetOrCreateStream(streamConsumer.StreamId)
                    .Events
                    .AddTimestamp(timestamp.Timestamp)
                    .AddValue("ticket", JsonSerializer.Serialize(ticket));

                // preferably some logic to avoid ticketing more than once per certain period
                // would be better user experience but is out of the scope of this sample
            }
        }
    };
};
```

### Support for stateful processing 

Quix Streams includes a state management feature that let's you store intermediate steps in complex calculations. Out of box you are provided with a RocksDB backed state. To use it, you can create an instance of `LocalFileStorage` or use one of our helper classes to manage the state such as `InMemoryStorage`. 
Here's an example of a stateful operation sum for a selected column in data.

``` csharp
consumer.OnStreamReceived += (sender, streamConsumer) =>
{
    // Create a dictionary for rolling sums, starting with 0
    // This would allow us to have any number of rolling sums for the stream
    // rather than just one
    var rollingSums = streamConsumer.GetDictionaryState("rolling_sums", (key) => 0d);
    
    // Scalar state when you do not need a dictionary
    var gforceMax = streamConsumer.GetScalarState("gforce_max", () => 0d);
    
    streamConsumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            var gforce = timestamp.Parameters["gforce"].NumericValue;
            if (gforce > gforceMax.Value)
            {
                gforceMax.Value = gforce.Value;
            }

            rollingSums["gforce"] += gforce ?? 0;
        }
    };
};

// App.SetStateStorageType(StateStorageTypes.InMemory);
// App.SetStateStorageType(StateStorageTypes.RocksDb); // The default
// App.SetStateStorageRootDir("./another_folder"); // the default is ./state
```

## Performance and Usability Enhancements

The library also includes a number of other enhancements that are designed to simplify the process of managing configuration and performance when interacting with Kafka:

- <b>No schema registry required</b>: Quix Streams doesn't need a schema registry to send different set of types or parameters, this is handled internally by the protocol. This means that you can send <b>more than one schema per topic</b><br>.

- <b>Message splitting</b>: Quix Streams automatically handles <b>large messages</b> on the producer side, splitting them up if required. You no longer need to worry about Kafka message limits. On the consumer side, those messages are automatically merged back.<br><br>

- <b>Message Broker configuration</b>: Many configuration settings are needed to use Kafka at its best, and the ideal configuration takes time. Quix Streams takes care of Kafka configuration by default but also supports custom configurations.<br><br>

- <b>Checkpointing</b>: Quix Streams supports manual or automatic checkpointing when you consume data from a Kafka Topic. This provides the ability to inform the Message Broker that you have already processed messages up to one point.<br><br>

- <b>Horizontal scaling</b>: Quix Streams handles horizontal scaling using the streaming context feature. You can scale the processing services, from one replica to many and back to one, and the library ensures that the data load is always shared between your replicas reliably.<br>

For a detailed overview of features, [visit our documentation](https://www.quix.io/docs/client-library-intro.html).

### What's Next

This library is being actively developed, however we have separated out the python library into https://github.com/quixio/quix-streams, where python version of 2.0 and above will be hosted. Because of this, you will find this repo only contains C#. If you're looking for the maintenance branch of 0.5.x, you can find it at https://github.com/quixio/quix-streams/tree/release/v0.5. We're going to maintain compatibility between the two versions and over time take new features developed in python to C#.


## Using Quix Streams with the Quix SaaS platform

This library doesn't have any dependency on any commercial product, but if you use it together with [Quix SaaS platform](https://www.quix.io) you will get some advantages out of the box during your development process such as auto-configuration, monitoring, data explorer, data persistence, pipeline visualization, metrics, and more.

## Contribution Guide

Contributing is a great way to learn and we especially welcome those who haven't contributed to an OSS project before. We're very open to any feedback or code contributions to this OSS project ❤️. Before contributing, please read our [Contributing File](https://github.com/quixio/quix-streams-dotnet/blob/main/CONTRIBUTING.md) and familiarize yourself with our [architecture](https://github.com/quixio/quix-streams-dotnet/blob/main/arch-notes.md) for how you can best give feedback and contribute. 

## Need help?

If you run into any problems, ask on #quix-help in [The Stream Slack channel](https://quix.io/slack-invite), alternatively create an [issue](https://github.com/quixio/quix-streams-dotnet/issues)


## Community 👭

Join other software engineers in our [slack](https://quix.io/slack-invite), an online community of people interested in all things data streaming. This is a space to both listen to and share learnings.

## License

Quix Streams is licensed under the Apache 2.0 license. View a copy of the License file [here](https://github.com/quixio/quix-streams-dotnet/blob/main/LICENSE).

## Stay in touch 👋

You can follow us on [Twitter](https://twitter.com/quix_io) and [Linkedin](https://www.linkedin.com/company/70925173) where we share our latest tutorials, forthcoming community events and the occasional meme.  

If you have any questions or feedback - write to us at support@quix.io!
