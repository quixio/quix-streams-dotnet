# Processing data

With Quix Streams, the main operations you need to learn are how to [subscribe](subscribe.md) to topics and how to [publish](publish.md) data.

The typical pattern for creating a service is to [subscribe](subscribe.md) to data in a topic, process it, and then [publish](publish.md) it to a topic. A series of such services can be connected together into a stream processing pipeline.

The following examples show how to process data in the Pandas data frame format, the format defined by the `TimeseriesData` class, and the event data format:

``` cs
streamConsumer.timeseries.OnDataReceived += (stream, args) =>
{
    var outputData = new TimeseriesData();

    // Calculate mean value for each second of data to effectively down-sample source topic to 1Hz.
    outputData.AddTimestamp(args.Data.Timestamps.First().Timestamp)
        .AddValue("ParameterA 10Hz", args.Data.Timestamps.Average(s => s.Parameters["ParameterA"].NumericValue.GetValueOrDefault()))
        .AddValue("ParameterA source frequency", args.Data.Timestamps.Count);

    // Send data back to the stream
    streamProducer.Timeseries.Publish(outputData);
};
```

!!! tip

	The [Quix Portal](https://portal.platform.quix.ai/self-sign-up) provides easy-to-use [open source samples](https://github.com/quixio/quix-samples) for reading, writing, and processing data. These samples work directly with your workspace topics. You can configure and deploy these samples in the Quix serverless environment using the Quix Portal UI. While the samples provide ready-made connectors and transforms you can use in your pipeline, you can also explore their code to see how they work, and adapt them to make your own custom connectors and transforms.
