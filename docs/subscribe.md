# Subscribing to data

Quix Streams enables you to subscribe to the data in your topics in real time. This documentation explains in detail how to do this.

## Connect to Quix

To subscribe to data from your Kafka topics, you need an instance of `KafkaStreamingClient`. To create an instance, use the following code:

=== "C\#"
	
	``` cs
	var client = new QuixStreams.Streaming.KafkaStreamingClient("127.0.0.1:9092");
	```

You can read about other ways to connect to your message broker in the [Connecting to a broker](connect.md) section.

## Create a topic consumer

Topics are central to stream processing operations. To subscribe to data in a topic you need an instance of `TopicConsumer`. This instance enables you to receive all the incoming streams on the specified topic. You can create an instance using the client’s `get_topic_consumer` method, passing the `TOPIC` as the parameter.

=== "C\#"
    
    ``` cs
    var topicConsumer = client.GetTopicConsumer(TOPIC);
    ```

### Consumer group

The [Consumer group](kafka.md#consumer-group) is a concept used when you want to [scale horizontally](features/horizontal-scaling.md). Each consumer group is identified using an ID, which you set optionally when opening a connection to the topic for reading:

=== "C\#"
    
    ``` cs
    var topicConsumer = client.GetTopicConsumer("{topic}","{your-consumer-group-id}");
    ```

This indicates to the message broker that all the replicas of your process will share the load of the incoming streams. Each replica only receives a subset of the streams coming into the Input Topic.

!!! warning

	If you want to consume data from the topic locally for debugging purposes, and the model is also deployed elsewhere, make sure that you change the consumer group ID to prevent clashing with the other deployment. If the clash happens, only one instance will be able to receive data for a partition at the same time.

## Subscribing to streams

=== "C\#"
    Once you have the `TopicConsumer` instance you can start consuming streams. For each stream received by the specified topic, `TopicConsumer` will execute the event `OnStreamReceived`. You can attach a callback to this event to execute code that reacts when you receive a new stream. For example, the following code prints the `StreamId` for each stream received by that Topic:
    
    ``` cs
    topicConsumer.OnStreamReceived += (topic, newStream) =>
    {
        Console.WriteLine($"New stream read: {newStream.StreamId}");
    };
    
    topicConsumer.Subscribe();
    ```

    !!! tip

    	The `Subscribe` method starts consuming streams and data from your topic. You should do this after you’ve registered callbacks for all the events you want to listen to. `App.run()` can also be used for this and provides other benefits. Find out more about [App.run()](app-management.md).

## Subscribing to time-series data

### TimeseriesData format

`TimeseriesData` is the formal class in Quix Streams that represents a time-series data packet in memory. The format consists of a list of timestamps, with their corresponding parameter names and values for each timestamp.

You can imagine a `TimeseriesData` as a table where the `Timestamp` is the first column of that table, and where the parameters are the columns for the values of that table. 

The following table shows an example:

| Timestamp | Speed | Gear |
| --------- | ----- | ---- |
| 1         | 120   | 3    |
| 2         | 123   | 3    |
| 3         | 125   | 3    |
| 6         | 110   | 2    |

=== "C\#"

    You can subscribe to time-series data from streams using the `OnDataReceived` event of the `StreamConsumer` instance. For instance, in the following example we consume and print the first timestamp and value of the parameter `ParameterA` received in the [TimeseriesData](#timeseriesdata-format) packet:
    
    ``` cs
    topicConsumer.OnStreamReceived += (topic, streamConsumer) =>
    {
        streamConsumer.Timeseries.OnDataReceived += (sender, args) =>
        {
            var timestamp = args.Data.Timestamps[0].Timestamp;
            var numValue = args.Data.Timestamps[0].Parameters["ParameterA"].NumericValue;
            Console.WriteLine($"ParameterA - {timestamp}: {numValue}");
        };
    };
    
    topicConsumer.Subscribe();
    ```

!!! note

    `subscribe()` starts consuming from the topic however, `App.run()` can also be used for this and provides other benefits.

    Find out more about [App.run()](app-management.md).

Quix Streams supports numeric, string, and binary value types. You should use the correct property depending of the value type of your parameter:

=== "C\#"
    
      - `NumericValue`: Returns the numeric value of the parameter, represented as a `double` type.    
      - `StringValue`: Returns the string value of the parameter, represented as a `string` type.    
      - `BinaryValue`: Returns the binary value of the parameter, represented as an array of `byte`.

This is a simple example showing how to consume `Speed` values of the `TimeseriesData` used in the previous example:

=== "C\#"

    ``` cs
    foreach (var timestamp in data.Timestamps)
    {
           var timestamp = timestamp.TimestampNanoseconds;
           var numValue = timestamp.Parameters["Speed"].NumericValue;
           Console.WriteLine($"Speed - {timestamp}: {numValue}");
    }
    ```

The output from this code is as follows:

``` console
Speed - 1: 120
Speed - 2: 123
Speed - 3: 125
Speed - 6: 110
```

### Raw data format

In addition to the `TimeseriesData`, there is also the raw data format. You can use the `OnRawReceived` event (C#) to handle this data format, as demonstrated in the following code:

=== "C\#"

    In C#, you typically use the raw format when you want to maximize performance: 

    ``` cs
	receivedStream.Timeseries.OnRawReceived += (sender, args) =>
	{
		streamWriter.Timeseries.Publish(args.Data);
	};
    ```

In C# `TimeseriesDataRaw` is mainly used for optimizing performance.

### Using a Buffer

Quix Streams provides you with an optional programmable buffer which you can configure to your needs. Using buffers to consume data enables you to process data in batches according to your needs. The buffer also helps you to develop models with a high-performance throughput.

=== "C\#"
    You can use the `Buffer` property embedded in the `Timeseries` property of your `stream`, or create a separate instance of that buffer using the `CreateBuffer` method:

    ``` cs
    var buffer = newStream.Timeseries.CreateBuffer();
    ```

You can configure a buffer’s input requirements using built-in properties. For example, the following configuration means that the Buffer will release a packet when the time span between first and last timestamp inside the buffer reaches 100 milliseconds:

=== "C\#"
    
    ``` cs
    buffer.TimeSpanInMilliseconds = 100;
    ```

Consuming data from that buffer is achieved by subscribing to events. The buffer uses the same events as when consuming without the buffer. For example, the following code prints the `ParameterA` value of the first timestamp of each packet released from the buffer:

=== "C\#"
    
    ``` cs
    buffer.OnDataReleased += (sender, args) =>
    {
        var timestamp = ags.Data.Timestamps[0].Timestamp;
        var numValue = ags.Data.Timestamps[0].Parameters["ParameterA"].NumericValue;
        Console.WriteLine($"ParameterA - {timestamp}: {numValue}");
    };
    ```

You can configure multiple conditions to determine when the buffer has to release data, if any of these conditions become true, the buffer will release a new packet of data and that data is cleared from the buffer:

=== "C\#"
    
      - `Buffer.BufferTimeout`: The maximum duration in milliseconds for which the buffer will be held before releasing the data. A packet of data is released when the configured timeout value has elapsed from the last data received in the buffer.    
      - `Buffer.PacketSize`: The maximum packet size in terms of number of timestamps. Each time the buffer has this amount of timestamps, the packet of data is released.    
      - `Buffer.TimeSpanInNanoseconds`: The maximum time between timestamps in nanoseconds. When the difference between the earliest and latest buffered timestamp surpasses this number, the packet of data is released.    
      - `Buffer.TimeSpanInMilliseconds`: The maximum time between timestamps in milliseconds. When the difference between the earliest and latest buffered timestamp surpasses this number, the packet of data is released. Note: This is a millisecond converter on top of `TimeSpanInNanoseconds`. They both work with the same underlying value.    
      - `Buffer.CustomTriggerBeforeEnqueue`: A custom function which is invoked **before** adding a new timestamp to the buffer. If it returns true, the packet of data is released before adding the timestamp to it.    
      - `Buffer.CustomTrigger`: A custom function which is invoked **after** adding a new timestamp to the buffer. If it returns true, the packet of data is released with the entire buffer content.    
      - `Buffer.Filter`: A custom function to filter the incoming data before adding it to the buffer. If it returns true, data is added, otherwise it isn’t.

#### Examples

The following buffer configuration sends data every 100ms or, if no data is buffered in the 1 second timeout period, it will empty the buffer and send the pending data anyway:

=== "C\#"
    
    ``` cs
    stream.Timeseries.Buffer.PacketSize = 100;
    stream.Timeseries.Buffer.BufferTimeout = 1000;
    ```

The following buffer configuration sends data every 100ms window, or if critical data arrives:

=== "C\#"
    
    ``` cs
    stream.Timeseries.Buffer.TimeSpanInMilliseconds = 100;
    stream.Timeseries.Buffer.CustomTrigger = data => data.Timestamps[0].Tags["is_critical"] == "True";
    ```
    
## Subscribing to events

`EventData` is the formal class in Quix Streams which represents an Event data packet in memory. `EventData` is meant to be used when the data is intended to be consumed only as single unit, such as JSON payload where properties can't be converted to individual parameters. `EventData` can also be better for non-standard changes, such as when a machine shutting down publishes an event named `ShutDown`.

!!! tip

	If your data source generates data at regular time intervals, or the information can be organized in a fixed list of Parameters, the [TimeseriesData](#timeseriesdata-format) format is a better fit for your time-series data.

### EventData format

`EventData` consists of a record with a `Timestamp`, an `EventId` and an `EventValue`.

You can imagine a list of `EventData` instances as a table of three columns where the `Timestamp` is the first column of that table and the `EventId` and `EventValue` are the second and third columns, as shown in the following table:

| Timestamp | EventId     | EventValue                 |
| --------- | ----------- | -------------------------- |
| 1         | failure23   | Gearbox has a failure      |
| 2         | box-event2  | Car has entered to the box |
| 3         | motor-off   | Motor has stopped          |
| 6         | race-event3 | Race has finished          |

Consuming events from a stream is similar to consuming timeseries data. In this case, the library does not use a buffer, but the way you consume Event Data from a stream is similar:

=== "C\#"
    
    ``` cs
    newStream.Events.OnDataReceived += (stream, args) =>
    {
        Console.WriteLine($"Event received for stream. Event Id: {args.Data.Id}");
    };
    ```

This generates the following output:

``` console
Event consumed for stream. Event Id: failure23
Event consumed for stream. Event Id: box-event2
Event consumed for stream. Event Id: motor-off
Event consumed for stream. Event Id: race-event3
```

## Responding to changes in stream properties

If the properties of a stream are changed, the consumer can detect this and handle it using the `on_changed` method.

You can write the handler as follows:

=== "C\#"

    ``` cs
    streamConsumer.Properties.OnChanged += (sender, args) =>
    {
        Console.WriteLine($"Properties changed for stream: {streamConsumer.StreamId}");	
    }
    ```

Then register the properties change handler:

=== "C\#"

    For C#, locate the properties changed handler inside the `OnStreamReceived` callback, for example:

    ``` cs
    topicConsumer.OnStreamReceived += (topic, streamConsumer) =>
    {
        streamConsumer.Timeseries.OnDataReceived += (sender, args) =>
        {
            Console.WriteLine("Data received");
        };

        streamConsumer.Properties.OnChanged += (sender, args) =>
        {
            Console.WriteLine($"Properties changed for stream: {streamConsumer.StreamId}");	
        }

    };

    topicConsumer.Subscribe();
    ```

You can keep a copy of the properties if you need to find out which properties have changed.

## Responding to changes in parameter definitions

It is possible to handle changes in [parameter definitions](./publish.md#parameter-definitions). Parameter definitions are metadata attached to data in a stream. The `on_definitions_changed` event is linked to an appropriate event handler, as shown in the following example code:

=== "C\#"

    ``` cs
    topicConsumer.OnStreamReceived += (topic, streamConsumer) =>
    {
        streamConsumer.Events.OnDataReceived += (sender, args) =>
        {
            Console.WriteLine("Data received");
        };

        streamConsumer.Events.OnDefinitionsChanged += (sender, args) =>
        {
            Console.WriteLine("Definitions changed");
        };

    };

    topicConsumer.Subscribe();
    ```

## Committing / checkpointing

It is important to be aware of the commit concept when working with a broker. Committing enables you to mark how far data has been processed, also known as creating a [checkpoint](kafka.md#checkpointing). In the event of a restart or rebalance, the client only processes messages from the last committed position. Commits are completed for each consumer group, so if you have several consumer groups in use, they do not affect each another when committing.

!!! tip

	Commits are done at a partition level when you use Kafka as a message broker. Streams that belong to the same partition are committed at the same time using the same position. Quix Streams currently does not expose the option to subscribe to only specific partitions of a topic, but commits will only ever affect partitions that are currently assigned to your client.

	Partitions and the Kafka rebalancing protocol are internal details of the Kafka implementation of Quix Streams. You can learn more about the Streaming Context feature of the library [here](features/streaming-context.md).

### Automatic committing

By default, Quix Streams automatically commits processed messages at a regular default interval, which is every 5 seconds or 5,000 messages, whichever happens sooner. However, this is subject to change.

If you need to use different automatic commit intervals, use the following code:

=== "C\#"
    
    ``` cs
    var topicConsumer = client.GetTopicConsumer(topic, consumerGroup, new CommitOptions()
    {
            CommitEvery = 100,
            CommitInterval = 500,
            AutoCommitEnabled = true // optional, defaults to true
    });
    ```

The code above commits every 100 processed messages or 500 ms, whichever is sooner.

### Manual committing

Some use cases need manual committing to mark completion of work, for example when you wish to batch process data, so the frequency of commit depends on the data. This can be achieved by first enabling manual commit for the topic:

=== "C\#"
    
    ``` cs
    client.GetTopicConsumer(topic, consumerGroup, CommitMode.Manual);
    ```

Then, whenever your commit condition is fulfilled, call:

=== "C\#"
    
    ``` cs
    topicConsumer.Commit();
    ```

The previous code commits parameters, events, or metadata that is consumed and served to you from the topic you subscribed to, up to this point.

### Committed and committing events

=== "C\#"

    Whenever a commit completes, an event is raised that can be connected to a handler. This event is raised for both manual and automatic commits. You can subscribe to this event using the following code:
    
    ``` cs
    topicConsumer.OnCommitted += (sender, args) =>
    {
        //... your code …
    };
    ```

While the `on_committed` event is triggered once the data has been committed, there is also the `on_committing` event which is triggered at the beginning of the commit cycle, should you need to carry out other tasks before the data is committed.

### Auto offset reset

You can control the offset that data is received from by optionally specifying `AutoOffsetReset` when you open the topic.

When setting the `AutoOffsetReset` you can specify one of three options:

| Option   | Description                                                      |
| -------- | ---------------------------------------------------------------- |
| Latest   | Receive only the latest data as it arrives, dont include older data |
| Earliest | Receive from the beginning, that is, as much as possible            |
| Error    | Throws exception if no previous offset is found                     |

The default option is `Latest`.

=== "C\#"
    
    ``` cs
    var topicConsumer = client.GetTopicConsumer("MyTopic", autoOffset: AutoOffsetReset.Latest);
    or
    var topicConsumer = client.GetTopicConsumer("MyTopic", autoOffset: AutoOffsetReset.Earliest);
    ```

## Revocation

When working with a broker, you have a certain number of topic streams assigned to your consumer. Over the course of the client’s lifetime, there may be several events causing a stream to be revoked, like another client joining or leaving the consumer group, so your application should be prepared to handle these scenarios in order to avoid data loss and/or avoidable reprocessing of messages.

!!! tip

	Kafka revokes entire partitions, but Quix Streams makes it easy to determine which streams are affected by providing two events you can listen to.

	Partitions and the Kafka rebalancing protocol are internal details of the Kafka implementation of Quix Streams. Quix Streams abstracts these details in the [Streaming Context](features/streaming-context.md) feature.

### Streams revoking

One or more streams are about to be revoked from your client, but you have a limited time frame, according to your broker configuration, to react to this and optionally commit to the broker:

=== "C\#"
    
    ``` cs
    topicConsumer.OnRevoking += (sender, args) =>
        {
            // ... your code ...
        };
    ```

### Streams revoked

One or more streams are revoked from your client. You can no longer commit to these streams, you can only handle the revocation in your client:

=== "C\#"
    
    ``` cs
    topicConsumer.OnStreamsRevoked += (sender, revokedStreams) =>
        {
            // revoked streams are provided to the handler
        };
    ```

## Stream closure

=== "C\#"

    You can detect stream closure with the stream closed event which has the sender and the `StreamEndType` to help determine the closure reason if required.
    
    ``` cs
    topicConsumer.OnStreamReceived += (topic, streamConsumer) =>
    {
            streamConsumer.OnStreamClosed += (reader, args) =>
            {
                    Console.WriteLine("Stream closed with {0}", args.EndType);
            };
    };
    ```

The `StreamEndType` can be one of:

| StreamEndType | Description                                                         |
| ------------- | ------------------------------------------------------------------- |
| Closed        | The stream was closed normally                                      |
| Aborted       | The stream was aborted by your code for your own reasons            |
| Terminated    | The stream was terminated unexpectedly while data was being written |

## Minimal example

This is a minimal code example you can use to receive data from a topic using Quix Streams:

=== "C\#"
    
    ``` cs
    using System;
    using System.Linq;
    using System.Threading;
    using QuixStreams.Streaming;
    using QuixStreams.Streaming.Configuration;
    using QuixStreams.Streaming.Models;
    
    
    namespace ReadHelloWorld
    {
        class Program
        {
            /// <summary>
            /// Main will be invoked when you run the application
            /// </summary>
            static void Main()
            {
                // Create a client which holds generic details for creating input and output topics
                var client = new KafkaStreamingClient("127.0.0.1:9092")
    
                using var topicConsumer = client.GetTopicConsumer(TOPIC_ID);
    
                // Hook up events before subscribing to avoid losing out on any data
                topicConsumer.OnStreamReceived += (topic, streamConsumer) =>
                {
                    Console.WriteLine($"New stream received: {streamConsumer.StreamId}");
    
                    var buffer = streamConsumer.Timeseries.CreateBuffer();
    
                    buffer.OnDataReleased += (sender, args) =>
                    {
                        Console.WriteLine($"ParameterA - {ags.Data.Timestamps[0].Timestamp}: {ags.Data.Timestamps.Average(a => a.Parameters["ParameterA"].NumericValue)}");
                    };
                };
    
                Console.WriteLine("Listening for streams");
    
                // Hook up to termination signal (for docker image) and CTRL-C and open streams
                App.Run();
    
                Console.WriteLine("Exiting");
            }
        }
    }
    ```

## Subscribing to raw Kafka messages

Quix Streams uses an internal protocol which is both data and speed optimized, but you do need to use the library for both the producer and consumer. Custom formats need to be handled manually. To enable this, the library provides the ability to [publish](publish.md#publish-raw-kafka-messages) and [subscribe](subscribe.md#subscribe-raw-kafka-messages) to the raw, unformatted messages, and to work with them as bytes. This gives you the means to implement the protocol as needed and convert between formats.

=== "C\#"
    
    ``` cs
    var rawConsumer = client.GetRawTopicConsumer(TOPIC_ID)
    
    rawConsumer.OnMessageRead += (sender, message) =>
    {
        var data = (byte[])message.Value;
    };
    
    rawConsumer.Subscribe()  // or use App.Run()
    ```
