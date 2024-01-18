#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer')

## IStreamTimeseriesProducer Interface

Helper class for producing [ParameterDefinition](ParameterDefinition.md 'QuixStreams.Streaming.Models.ParameterDefinition') and [TimeseriesData](TimeseriesData.md 'QuixStreams.Streaming.Models.TimeseriesData')

```csharp
public interface IStreamTimeseriesProducer :
System.IDisposable
```

Derived  
&#8627; [StreamTimeseriesProducer](StreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [Buffer](IStreamTimeseriesProducer.Buffer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Buffer') | Gets the buffer for producing timeseries data |
| [DefaultLocation](IStreamTimeseriesProducer.DefaultLocation.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.DefaultLocation') | Default Location of the parameters. Parameter definitions added with [AddDefinition(string, string, string)](IStreamTimeseriesProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddDefinition(string, string, string)') will be inserted at this location.<br/>See [AddLocation(string)](IStreamTimeseriesProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddLocation(string)') for adding definitions at a different location without changing default.<br/>Example: "/Group1/SubGroup2" |

| Methods | |
| :--- | :--- |
| [AddDefinition(string, string, string)](IStreamTimeseriesProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddDefinition(string, string, string)') | Adds a new parameter definition to the [StreamTimeseriesProducer](StreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducer'). Configure it with the builder methods. |
| [AddDefinitions(List&lt;ParameterDefinition&gt;)](IStreamTimeseriesProducer.AddDefinitions(List_ParameterDefinition_).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddDefinitions(System.Collections.Generic.List<QuixStreams.Streaming.Models.ParameterDefinition>)') | Adds a list of definitions to the [StreamTimeseriesProducer](StreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducer'). Configure it with the builder methods. |
| [AddLocation(string)](IStreamTimeseriesProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddLocation(string)') | Adds a new location in the parameters groups hierarchy |
| [Flush()](IStreamTimeseriesProducer.Flush().md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Flush()') | Immediately publish timeseries data and definitions from the buffer without waiting for buffer condition to fulfill for either |
| [Publish(TimeseriesData)](IStreamTimeseriesProducer.Publish(TimeseriesData).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Publish(QuixStreams.Streaming.Models.TimeseriesData)') | Publish data to stream without any buffering |
| [Publish(TimeseriesDataTimestamp)](IStreamTimeseriesProducer.Publish(TimeseriesDataTimestamp).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Publish(QuixStreams.Streaming.Models.TimeseriesDataTimestamp)') | Publish single timestamp to stream without any buffering |
| [Publish(TimeseriesDataRaw)](IStreamTimeseriesProducer.Publish(TimeseriesDataRaw).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.Publish(QuixStreams.Telemetry.Models.TimeseriesDataRaw)') | Publish data in TimeseriesDataRaw format without any buffering |
