# Quickstart

Quix Streams provides you with a library for developing real-time streaming applications focused on time-series data.

If you would like to know more about Quix Streams, you can view the [Quix Streams GitHub repository](https://github.com/quixio/quix-streams-dotnet){target=_blank}. Quix Streams is open source under the Apache 2.0 license.

In this quickstart guide you will learn how to start using Quix Streams as quickly as possible. This guide covers how to:

* Create a consumer
* Create a producer
* Create a producer/consumer transform 
* Connect to the Quix Platform

The typical stream processing pipline you create with Quix Streams involves producers, consumers, and transforms. Producers publish information into a topic, consumers subscribe to read information from a topic. Transforms typically consume data, process it in some way, and then publish the transformed data to a topic, or stream within a topic.

In this guide you'll learn how to create a producer that publishes data to a topic, a consumer that reads data from a topic, and a simple transform that consumes data from a topic, transforms it, and then publishes the new data to a topic.

Initially you will work with your local Kafka installation, and then you'll learn how you can connect to Quix Platform. In Quix Platform you can build your stream processing pipelines graphically.

## Prerequisites

You have a local installation of [Kafka](https://kafka.apache.org/documentation/){target=_blank} up and running. You use this to test your code.

Optionally:

Sign up for a free [Quix account](https://portal.platform.quix.io/self-sign-up){target=_blank}. You may just want to connect to your own Kafka installation, but if you'd like to connect to the Quix Platform you'll need a free account.

## Getting help

If you need help with this guide, then please join our public Slack community [`The Stream`](https://quix.io/slack-invite){target=_blank}, and ask any questions you have there.

## Install

Follow instructions on https://www.nuget.org/packages/QuixStreams.Streaming or install it using your favourite IDE.

!!! note

    The following sections assume you have a local installation of [Kafka](https://kafka.apache.org/){target=_blank} running. 

## Create a Consumer

To create a simple consumer, follow these steps:

1. Create a new dotnet project

2. Update your Program.cs to the following code

    ``` csharp
    using QuixStreams.Streaming;

    // Client connecting to Kafka instance locally without authentication. 
    var client = new KafkaStreamingClient("127.0.0.1:9092");

    // Open the topic to consume from, without any consumer group
    var topicConsumer = client.GetTopicConsumer("quickstart-topic");

    topicConsumer.OnStreamReceived += (sender, consumer) =>
    {
        consumer.Timeseries.OnDataReceived += (o, eventArgs) =>
        {
            // process eventArgs.Data
            Console.WriteLine($"Data Received with {eventArgs.Data.Timestamps.Count} timestamps!");
        };
    };

    Console.WriteLine("Listening to streams. Press CTRL-C to exit.");
    // Handle graceful exit
    App.Run();
    ```

3. Run the code!

The code will wait for published messages and then print information about any messages received to the console. You'll next build a suitable producer than can publish messages to the example topic.

## Create a Producer

To create a simple producer follow these steps:

1. Start a new project, preferably in a new IDE window so you will be able to run both previous consumer and this.

2. Update your Program.cs to the following code

    ``` csharp
    using System.Text;
    using QuixStreams.Streaming;

    // Client connecting to Kafka instance locally without authentication. 
    var client = new KafkaStreamingClient("127.0.0.1:9092");

    // Open the topic where we produce data to.
    var topicProducer = client.GetTopicProducer("quickstart-topic");

    var stream = topicProducer.CreateStream();
    stream.Properties.Name = "Hello World python stream";
    stream.Properties.Metadata["my-metadata"] = "my-metadata-value";
    stream.Timeseries.Buffer.TimeSpanInMilliseconds = 100; // Send data in 100 ms chunks

    var end = DateTime.UtcNow.AddSeconds(30);
    var nextLog = DateTime.UtcNow;
    var counter = 0;
    while (DateTime.UtcNow < end)
    {
        if (nextLog < DateTime.UtcNow)
        {
            Console.WriteLine($"Sending values for ~{Math.Ceiling((end - DateTime.UtcNow).TotalSeconds)} more seconds.");
            nextLog = DateTime.UtcNow.AddSeconds(5);
        }

        stream.Timeseries
            .Buffer
            .AddTimestamp(DateTime.UtcNow)
            .AddValue("ParameterA", Math.Sin(counter / 200.0))
            .AddValue("ParameterB", "string value: " + counter)
            .AddValue("ParameterC", Encoding.UTF8.GetBytes("bye value: " + counter))
            .Publish();

        Thread.Sleep(30);
        counter++;
    }

    Console.WriteLine("Closing stream");
    stream.Close(); // or just using
    topicProducer.Dispose(); // or just using
    ```

3. Run the code!
4. Also run the consumer example to see that working.

## Consumer-producer transform

Typically a transform block in Quix will receive some data on an input topic, perform some processing on the data, and then publish data to an output topic. Example code that does this is shown here:

1. Start a new project, preferably in a new IDE window so you will be able to run both previous producer and this

2. Update your Program.cs to the following code

    ``` csharp
    using QuixStreams.Streaming;

    // Client connecting to Kafka instance locally without authentication. 
    var client = new KafkaStreamingClient("127.0.0.1:9092");

    // Open the topics we consume from or produce to
    var topicConsumer = client.GetTopicConsumer("quickstart-topic");
    var topicProducer = client.GetTopicProducer("quickstart-transformed");


    topicConsumer.OnStreamReceived += (sender, consumer) =>
    {
        // using GetOrCreateStream you can retrieve already existing entity in memory without having to recreate it
        var transformedSteam = topicProducer.GetOrCreateStream(consumer.StreamId + "-transformed", newStream =>
        {
            // and can do one time setup in case it did not yet exist and had to be created
        });
        consumer.Timeseries.OnDataReceived += (o, eventArgs) =>
        {
            // Apply any transformation to the data, in our case
            // we simply pass to output stream
            Console.WriteLine($"Publishing data to output stream {transformedSteam.StreamId}");
            transformedSteam.Timeseries.Publish(eventArgs.Data);
        };
    };

    Console.WriteLine("Listening to streams. Press CTRL-C to exit.");
    // Handle graceful exit
    App.Run();
    ```

3. Run the code!
4. Also run the producer example to see the transformation logic execute.
5. Optionally update your original consumer example to listen to `quickstart-transformed` to see that working.

This example reads data in from the `quickstart-topic` topic, and then writes the transformed data out to the `quickstart-transformed` topic.

This approach of consuming, transforming, and producing data is a fundamental of building data processing pipelines.

## Connecting to Quix Platform

As well as being able to connect directly to a Kafka installation, either locally (for development purposes), on premise, or in the cloud, you can also connect to the Quix Platform, the SaaS for building real-time stream processing applications. Quix Platform provides the ability to build stream processing applications in a graphical environment, and deploy the applications to the Quix-hosted infrastructure.

### Obtaining a token

To connect to the Quix Platform using Quix Streams, you will need to provide a token for authentication.

1. Sign up for a free [Quix account](https://portal.platform.quix.io/self-sign-up){target=_blank}, and log in.

2. In the Quix Platform, click on `Topics` on the left-hand navigation. 

3. Click on the gear icon. The `Broker Settings` dialog is displayed. 

4. Copy `token 1` to the clipboard. You will use that in the code that connects to the Quix platform.

### Code to connect to Quix Platform

The following code snippet shows you how to connect to the Quix Platform:

``` csharp
// Client connecting to kafka using Quix Streaming client to retrieve authentication details
var client = new QuixStreamingClient("<your-token>"); // token can be obtained from UI (either PAT or streaming token)
```

This connects to the Quix Platform, rather than your local Kafka installation, which is the code you saw previously in this guide.

A further example is to rewrite the consumer-producer program you created earlier in this Quickstart, to work with Quix Platform:

``` csharp
using QuixStreams.Streaming;

// Client connecting to kafka using Quix Streaming client to retrieve authentication details
var client = new QuixStreamingClient("<your-token>");

// Open the topics we consume from or produce to
var topicConsumer = client.GetTopicConsumer("quickstart-topic");
var topicProducer = client.GetTopicProducer("quickstart-transformed");


topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    // using GetOrCreateStream you can retrieve already existing entity in memory without having to recreate it
    var transformedSteam = topicProducer.GetOrCreateStream(consumer.StreamId + "-transformed", newStream =>
    {
        // and can do one time setup in case it did not yet exist and had to be created
    });
    consumer.Timeseries.OnDataReceived += (o, eventArgs) =>
    {
        // Apply any transformation to the data, in our case
        // we simply pass to output stream
        Console.WriteLine($"Publishing data to output stream {transformedSteam.StreamId}");
        transformedSteam.Timeseries.Publish(eventArgs.Data);
    };
};

Console.WriteLine("Listening to streams. Press CTRL-C to exit.");
// Handle graceful exit
App.Run();
```

## Next steps

Try one of the following resources to continue your Quix learning journey:

* [Get a free Quix account](https://portal.platform.quix.io/self-sign-up){target=_blank}

* [Quix Streams GitHub](https://github.com/quixio/quix-streams-dotnet){target=_blank}

* [Quix Platform glossary](https://quix.io/docs/platform/glossary.html)

* [The Stream community on Slack](https://quix.io/slack-invite){target=_blank}

* [Stream processing glossary](https://quix.io/stream-processing-glossary/){target=_blank}

* [Sentiment analysis tutorial](https://quix.io/docs/platform/tutorials/sentiment-analysis/index.html)

* [Kafka setup blog post](https://www.quix.io/blog/send-timeseries-data-to-kafka-python/?returnUrl=https://www.quix.io/blog/tutorial/){target=_blank}
