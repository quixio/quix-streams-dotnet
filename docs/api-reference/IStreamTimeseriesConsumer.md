#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamConsumer](QuixStreams.Streaming.Models.StreamConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer')

## IStreamTimeseriesConsumer Interface

Consumer for streams, which raises [TimeseriesData](TimeseriesData.md 'QuixStreams.Streaming.Models.TimeseriesData') and [QuixStreams.Telemetry.Models.ParameterDefinitions](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Telemetry.Models.ParameterDefinitions 'QuixStreams.Telemetry.Models.ParameterDefinitions') related messages

```csharp
public interface IStreamTimeseriesConsumer :
System.IDisposable
```

Derived  
&#8627; [StreamTimeseriesConsumer](StreamTimeseriesConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer.StreamTimeseriesConsumer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [Definitions](IStreamTimeseriesConsumer.Definitions.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.Definitions') | Gets the latest set of parameter definitions |

| Methods | |
| :--- | :--- |
| [CreateBuffer(TimeseriesBufferConfiguration, string[])](IStreamTimeseriesConsumer.CreateBuffer(TimeseriesBufferConfiguration,string[]).md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.CreateBuffer(QuixStreams.Streaming.Models.TimeseriesBufferConfiguration, string[])') | Creates a new buffer for reading data |
| [CreateBuffer(string[])](IStreamTimeseriesConsumer.CreateBuffer(string[]).md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.CreateBuffer(string[])') | Creates a new buffer for reading data |

| Events | |
| :--- | :--- |
| [OnDataReceived](IStreamTimeseriesConsumer.OnDataReceived.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.OnDataReceived') | Event raised when data is received (without buffering)<br/>This event does not use Buffers, and data will be raised as they arrive without any processing. |
| [OnDefinitionsChanged](IStreamTimeseriesConsumer.OnDefinitionsChanged.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.OnDefinitionsChanged') | Raised when the parameter definitions have changed for the stream.<br/>See [Definitions](StreamTimeseriesConsumer.Definitions.md 'QuixStreams.Streaming.Models.StreamConsumer.StreamTimeseriesConsumer.Definitions') for the latest set of parameter definitions |
| [OnRawReceived](IStreamTimeseriesConsumer.OnRawReceived.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamTimeseriesConsumer.OnRawReceived') | Event raised when data is received (without buffering) in raw transport format<br/>This event does not use Buffers, and data will be raised as they arrive without any processing. |
