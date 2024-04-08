# Using Quix Streams

In the following documentation you will learn how to use Quix Streams to perform two types of data processing:

1. **Stateless processing** - Here one message is processed at a time, and the message received contains all required data for processing. No state needs to be preserved between messages, or between replicas. The data from the message is used to calculate new information, which is then published to the output stream. 
2. **Stateful processing** - This is where you need to keep track of data between messages, such as keeping a running total of a variable. This is more complicated as state needs to be preserved between messages, and potentially between replicas, where multiple replicas are deployed. In addition, state may need to be preserved in the event of the failure of a deployment - Quix Streams supports checkpointing as a way to enable this.

The following sections will explore these methods of data processing in more detail.

## Topics, streams, partitions, replicas, and consumer groups

The main structure used for data organization in Quix is the topic. For example, the topic might be `iot-telemetry`. To enable for horizontal scaling, a topic is typically divided into multiple streams. You may have multiple devices, or sources, writing data into a topic, so to ensure scaling and message ordering, each source writes into its own stream. Device 1 would write to stream 1, and device 2 to stream 2 and so on. This is the idea of [stream context](./features/streaming-context.md). 

In some use cases you may want to aggregate data beyond the stream context of a source. This can be done by writing data to new streams in the pipeline, based on a different key. For example, imagine a scenario where invoices are being streamed from stores, and where each stream is based on `StoreId`. Now, let's say you want to calculate totals of a specific item sold across all stores (streams). To do this, you can create a transform that first writes invoices into new streams based on `StockCode`, and then another transform can perform aggregation for each of these `StockCode` streams, in order to calculate how much of each item was sold.

Quix Streams ensures that stream context is preserved, that is, messages inside one stream are always published to the same single partition. This means that inside one stream, a consumer can rely on the order of messages. A partition can contain multiple streams, but a stream is always confined to one partition.

It is possible to organize the code that processes the streams in a topic using the idea of a consumer group. This indicates to the broker that you will process the topic with all available replicas in the consumer group, sharing the processing of all streams in the topic. Horizontal scaling occurs automatically, because when you deploy multiple replicas in a consumer group, a stream (or group of streams) is assigned to a replica. For example, if there are three streams and three replicas, each replica will process a single stream. This is illustrated in the following diagram:

![Consumer group](./images/QuixStreamsConsumerGroup.png)

If you had only one replica, it would need to process all streams in that topic. If you have three streams and two replicas, one replica would process two streams, and the other replica a single stream. If you don't specify a consumer group in your code, then all streams in a topic will be processed by all replicas. 

When you create the consumer you specify the consumer group as follows:

``` csharp
var topicConsumer = client.GetTopicConsumer("my-topic",
    consumerGroup: "my-service-consumergroup", // the unique consumer group for the service
    autoOffset: AutoOffsetReset.Earliest); // instruction to consume from beginning if no existing offset is found. Latest by default
```

Best practice is to make sure the consumer group name matches the name of the service to avoid overlaps when reading from same topic.

!!! warning

    If you don't specify a consumer group, then all messages in a topic will be processed by all replicas in the microservice deployment.

For further information read about how [Quix Streams works with Kafka](kafka.md).

## Stream data formats

There are two main formats of stream data: 

1. **Event data** - in Quix Streams this is represented with the `qx.EventData` class.
2. **Time-series** - in Quix Streams this is represented with the `qx.TimeseriesData` class (and two other classes: one for Pandas data frame format, and one for raw Kafka data).

Event data refers to data that is independent, whereas time-series data is a variable that changes over time. An example of event data is a financial transaction. It contains all data for the invoice, with a timestamp (the time of the transaction), but a financial transaction itself is not a variable you'd track over time. The invoice may itself contain time-series data though, such as the customer's account balance. 

Time-series data is a variable that is tracked over time, such as temperature from a sensor, or the g-forces in a racing car.

Time-series data has two different representations in Quix Streams, to serve different use cases and developers. The underlying data that these models represent is the same however. These are:

1. TimeseriesData: a row based approach to accessing data with significant number of sanity and safety checks
3. TimeseriesDataRaw: column based approach to accessing data without much sanity or safety checks. Intended for performance critical workloads.

In the following sections of this documentation you'll learn about these formats.

## Registering a callback for stream data

When it comes to registering your event handlers, the first step is to register a stream eventhandler that is invoked when the first message is received for a stream.

``` csharp
topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    // .. handler logic ..
}
```

The `OnStreamReceived` is typically written to handle a specific data format on that stream. This is explained in the next section.

!!! note

    This handler is invoked for each stream in a topic. This means you will have multiple instances of this callback invoked, if there are multiple streams. 

## Registering handlers to deal with data formats

Specific handlers are registered to work with each type of stream data.

The following table documents which handlers to register, depending on the type of stream data you need to handle:

| Stream data format | Callback to register |
|----|----|
| Event data | `consumer.Events.OnDataReceived += ...` |
| Time-series data | `consumer.Timeseries.OnDataReceived += ...` |
| Time-series raw data | `consumer.Timeseries.OnRawReceived +=  ...` |

!!! note

    You can have multiple handlers registered at the same time, but usually you would work with the data format most suited to your use case. For example, if the source was providing only event data, it only makes sense to register the event data callback.

### Example of handler registration

The following code sample demonstrates how to register a callback to handle data in the data frame format: 

``` csharp
topicConsumer.OnStreamReceived += (sender, newStreamConsumer) =>
{
    newStreamConsumer.Timeseries.OnDataReceived += (o, args) =>
    {
        // do things
    };
};
```

!!! note

    The handler is registered *only* for the specified stream.

## Stateless processing

Now that you have learned about stream data formats and callbacks, the following example shows a simple data processor. This will be an example of stateless processing, where messages are processed one at a time, and contain all information required for that processing.

This processor receives (consumes) data, processes it (transforms), and then publishes generated data (produces) on an output topic. This encapsulates the typical processing pipeline which consists of:

1. Consumer (reads data)
2. Transformer (processes data)
3. Producer (writes data)

The example code demonstrates this:

``` csharp
using QuixStreams.Streaming;
using QuixStreams.Telemetry.Kafka;

var client = new KafkaStreamingClient("127.0.0.1:9092");

var topicConsumer = client.GetTopicConsumer("input-topic",
    consumerGroup: "my-service-consumer-group",
    autoOffset: AutoOffsetReset.Earliest);
var topicProducer = client.GetTopicProducer("output-topic");


topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    var producerStream = topicProducer.GetOrCreateStream(consumer.StreamId);
    consumer.Timeseries.OnDataReceived += (o, args) =>
    {
        // optionally you can also clone it if you prefer over modifying original payload
        // var cloned = args.Data.Clone();
        foreach (var timestamp in args.Data.Timestamps)
        {
            var total = Math.Abs(timestamp.Parameters["gforce_x"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_y"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_z"].NumericValue!.Value);
            timestamp.AddValue("gforce_total", total);
        }
        
        producerStream.Timeseries.Publish(args.Data);
    };
};

Console.WriteLine("Listening to streams. Press CTRL-C to exit.");
// Handle graceful exit
App.Run();
```
Note that all information required to calculate `gForceTotal` is contained in the inbound data (the X, Y, and Z components of g-force). This is an example of stateless, or "one message at a time", processing: no state needs to be preserved between messages. 

Further, if multiple replicas were used here, it would require no changes to your code, as each replica, running its own instance of the callback for the target stream, would simply calculate a value for `gForceTotal` based on the data in the data frame it received.

## Stateful processing

With stateful processing, additional complexity is introduced, as data now needs to be preserved between messages, streams, and potentially replicas (where multiple replicas are deployed to handle multiple streams). 

## The problem of using global variables to track state

There are problems with using global variables in your code to track state. The first is that callbacks are registered per-stream. This means that if you modify a global variable in a callback, it will be modified by all streams. 

For example, consider the following **problematic** code:

``` csharp
...

topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    var gForceRunningTotal = 0.0;
    var producerStream = topicProducer.GetOrCreateStream(consumer.StreamId);
    consumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            var total = Math.Abs(timestamp.Parameters["gforce_x"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_y"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_z"].NumericValue!.Value);
            timestamp.AddValue("gforce_total", total);
            gForceRunningTotal = +total;
        }
        
        producerStream.Timeseries.Publish(args.Data);
    };
};
...
```

!!! warning
    With the previous example code, all streams modify the global variable.

You will quickly realise this will give you the total across all streams, rather than individually. You could move the definition of `gForceRunningTotal` within the `OnStreamReceived` callback, but there are use cases where a total across all streams might be what you want.

If you were running across multiple replicas, you'd get a running total for each replica, because each replica would have its own instance of the global variable. Again, the results would not be as you might expect.

Let's say there were three streams and two replicas, you'd get the running total of two streams for one replica, and the running total for the third stream in the other replica.

In most practical scenarios you'd want to track a running total per stream (say, total g-forces per race car), or perhaps for some variables a running total across all streams. Each of these scenarios is described in the followng sections.

## Handling system restarts and crashes

One issue you may run into is that in-memory data is not persisted across instance restarts, shutdowns, and instance crashes. This can be mitigated by using the Quix Streams state facility. This will ensure that specified variables are persisted on permanent storage, and this data is preserved across restarts, shutdowns, and system crashes.

The following example code demonstrates a simple use of state to **persist data across system restarts and crashes**:

``` csharp
...
topicConsumer.OnStreamReceived += (sender, consumer) =>
{
    var producerStream = topicProducer.GetOrCreateStream(consumer.StreamId);
    var stateManager = consumer.GetStateManager();
    var runningTotal = stateManager.GetScalarState("gforce_total", () => 0.0);
    consumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            var total = Math.Abs(timestamp.Parameters["gforce_x"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_y"].NumericValue!.Value) +
                        Math.Abs(timestamp.Parameters["gforce_z"].NumericValue!.Value);
            timestamp.AddValue("gforce_total", total);
            runningTotal.Value += total;
            timestamp.AddValue("gforce_running_total", runningTotal.Value);
        }
        
        producerStream.Timeseries.Publish(args.Data);
    };
};
...
```

This ensures that the variable `gforce_running_total` is persisted when the offset you read is committed.

If the system crashes (or is restarted), Kafka resumes message processing from the last committed offset. This facility is built into Kafka.

!!! tip

    For this facility to work in Quix Cloud you need to enable the State Management feature. You can enable it in the `Deployment` dialog, where you can also specify the size of storage required. When using Quix Streams with a third-party broker such as Kafka, no configuration is required, and data is automatically stored on the local file system.

## Tracking computed values across multiple streams

Sometimes you want to track a computed value across all streams in a topic such as running total in the previous example. The problem is that when you scale using replicas, there is no way simple way to share data between all replicas in a consumer group. Each replica will only consume a segment of the overall data and calculate the running average on it.

The solution is to write the computed value to a different topic, then consume that with single a downstream application. Preferably this would not be a computation heavy task as that should be done in the service with scaling capability.

## Conclusion

In this documentation you have learned:

* How to perform stateless "one message at a time" processing.
* How to handle the situation where state needs to be preserved, and problems that can arise in naive code.
* How to persist state, so that your data is preserved in the event of restarts or crashes.

## Next steps

Continue your Quix Streams learning journey by reading the following more in-depth documentation:

* [Publishing data](publish.md)
* [Subscribing to data](subscribe.md)
* [Processing data](process.md)
* [State management](state-management.md)
