#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamConsumer](QuixStreams.Streaming.Models.StreamConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer')

## IStreamEventsConsumer Interface

Interface for consumer of event streams, which raises [EventData](EventData.md 'QuixStreams.Streaming.Models.EventData') and [QuixStreams.Telemetry.Models.EventDefinitions](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Telemetry.Models.EventDefinitions 'QuixStreams.Telemetry.Models.EventDefinitions') related messages

```csharp
public interface IStreamEventsConsumer :
System.IDisposable
```

Derived  
&#8627; [StreamEventsConsumer](StreamEventsConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer.StreamEventsConsumer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [Definitions](IStreamEventsConsumer.Definitions.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamEventsConsumer.Definitions') | Gets the latest set of event definitions |

| Events | |
| :--- | :--- |
| [OnDataReceived](IStreamEventsConsumer.OnDataReceived.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamEventsConsumer.OnDataReceived') | Raised when an events data package is received for the stream |
| [OnDefinitionsChanged](IStreamEventsConsumer.OnDefinitionsChanged.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamEventsConsumer.OnDefinitionsChanged') | Raised when the event definitions have changed for the stream.<br/>See [Definitions](StreamEventsConsumer.Definitions.md 'QuixStreams.Streaming.Models.StreamConsumer.StreamEventsConsumer.Definitions') for the latest set of event definitions |
