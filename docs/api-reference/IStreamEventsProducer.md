#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer')

## IStreamEventsProducer Interface

Helper class for producing [QuixStreams.Telemetry.Models.EventDefinitions](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Telemetry.Models.EventDefinitions 'QuixStreams.Telemetry.Models.EventDefinitions') and [EventData](EventData.md 'QuixStreams.Streaming.Models.EventData')

```csharp
public interface IStreamEventsProducer :
System.IDisposable
```

Derived  
&#8627; [StreamEventsProducer](StreamEventsProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamEventsProducer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [DefaultLocation](IStreamEventsProducer.DefaultLocation.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.DefaultLocation') | Default Location of the events. Event definitions added with [AddDefinition(string, string, string)](IStreamEventsProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddDefinition(string, string, string)') will be inserted at this location.<br/>See [AddLocation(string)](IStreamEventsProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddLocation(string)') for adding definitions at a different location without changing default.<br/>Example: "/Group1/SubGroup2" |
| [DefaultTags](IStreamEventsProducer.DefaultTags.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.DefaultTags') | Default Tags injected to all Event Values sent by the producer. |
| [Epoch](IStreamEventsProducer.Epoch.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.Epoch') | Default epoch used for Timestamp event values. Datetime added on top of all the Timestamps. |

| Methods | |
| :--- | :--- |
| [AddDefinition(string, string, string)](IStreamEventsProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddDefinition(string, string, string)') | Add new Event definition to define properties like Name or Level, among others. |
| [AddDefinitions(List&lt;EventDefinition&gt;)](IStreamEventsProducer.AddDefinitions(List_EventDefinition_).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddDefinitions(System.Collections.Generic.List<QuixStreams.Streaming.Models.EventDefinition>)') | Adds a list of definitions to the [StreamEventsProducer](StreamEventsProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamEventsProducer'). Configure it with the builder methods. |
| [AddLocation(string)](IStreamEventsProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddLocation(string)') | Adds a new Location in the event groups hierarchy. |
| [AddTimestamp(DateTime)](IStreamEventsProducer.AddTimestamp(DateTime).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddTimestamp(System.DateTime)') | Starts adding a new set of event values at the given timestamp.<br/>Note, [Epoch](StreamEventsProducer.Epoch.md 'QuixStreams.Streaming.Models.StreamProducer.StreamEventsProducer.Epoch') is not used when invoking with [System.DateTime](https://docs.microsoft.com/en-us/dotnet/api/System.DateTime 'System.DateTime') |
| [AddTimestamp(TimeSpan)](IStreamEventsProducer.AddTimestamp(TimeSpan).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddTimestamp(System.TimeSpan)') | Starts adding a new set of event values at the given timestamp. |
| [AddTimestampMilliseconds(long)](IStreamEventsProducer.AddTimestampMilliseconds(long).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddTimestampMilliseconds(long)') | Starts adding a new set of event values at the given timestamp. |
| [AddTimestampNanoseconds(long)](IStreamEventsProducer.AddTimestampNanoseconds(long).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddTimestampNanoseconds(long)') | Starts adding a new set of event values at the given timestamp. |
| [Flush()](IStreamEventsProducer.Flush().md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.Flush()') | Immediately writes the event definitions from the buffer without waiting for buffer condition to fulfill (200ms timeout) |
| [Publish(EventData)](IStreamEventsProducer.Publish(EventData).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.Publish(QuixStreams.Streaming.Models.EventData)') | Publish an event into the stream. |
| [Publish(ICollection&lt;EventData&gt;)](IStreamEventsProducer.Publish(ICollection_EventData_).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.Publish(System.Collections.Generic.ICollection<QuixStreams.Streaming.Models.EventData>)') | Publish events into the stream. |
