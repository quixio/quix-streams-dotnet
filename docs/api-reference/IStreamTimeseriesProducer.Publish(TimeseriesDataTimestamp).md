#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[IStreamTimeseriesProducer](IStreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer')

## IStreamTimeseriesProducer.Publish(TimeseriesDataTimestamp) Method

Publish single timestamp to stream without any buffering

```csharp
void Publish(QuixStreams.Streaming.Models.TimeseriesDataTimestamp timestamp);
```
#### Parameters

<a name='QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Publish(QuixStreams.Streaming.Models.TimeseriesDataTimestamp).timestamp'></a>

`timestamp` [TimeseriesDataTimestamp](TimeseriesDataTimestamp.md 'QuixStreams.Streaming.Models.TimeseriesDataTimestamp')

Timeseries timestamp to publish