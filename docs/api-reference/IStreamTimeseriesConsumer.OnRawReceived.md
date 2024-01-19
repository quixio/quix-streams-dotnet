#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamConsumer](QuixStreams.Streaming.Models.StreamConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer').[IStreamTimeseriesConsumer](IStreamTimeseriesConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer')

## IStreamTimeseriesConsumer.OnRawReceived Event

Event raised when data is received (without buffering) in raw transport format  
This event does not use Buffers, and data will be raised as they arrive without any processing.

```csharp
event EventHandler<TimeseriesDataRawReadEventArgs> OnRawReceived;
```

#### Event Type
[System.EventHandler&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.EventHandler-1 'System.EventHandler`1')[TimeseriesDataRawReadEventArgs](TimeseriesDataRawReadEventArgs.md 'QuixStreams.Streaming.Models.StreamConsumer.TimeseriesDataRawReadEventArgs')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.EventHandler-1 'System.EventHandler`1')