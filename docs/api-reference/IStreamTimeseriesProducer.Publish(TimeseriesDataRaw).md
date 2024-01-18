#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[IStreamTimeseriesProducer](IStreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer')

## IStreamTimeseriesProducer.Publish(TimeseriesDataRaw) Method

Publish data in TimeseriesDataRaw format without any buffering

```csharp
void Publish(QuixStreams.Telemetry.Models.TimeseriesDataRaw data);
```
#### Parameters

<a name='QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Publish(QuixStreams.Telemetry.Models.TimeseriesDataRaw).data'></a>

`data` [QuixStreams.Telemetry.Models.TimeseriesDataRaw](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Telemetry.Models.TimeseriesDataRaw 'QuixStreams.Telemetry.Models.TimeseriesDataRaw')

Timeseries data to publish