#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer')

## IStreamPropertiesProducer Interface

Represents properties and metadata of the stream.  
All changes to these properties are automatically published to the underlying stream.

```csharp
public interface IStreamPropertiesProducer :
System.IDisposable
```

Derived  
&#8627; [StreamPropertiesProducer](StreamPropertiesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamPropertiesProducer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [FlushInterval](IStreamPropertiesProducer.FlushInterval.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.FlushInterval') | Automatic flush interval of the properties metadata into the channel [ in milliseconds ] |
| [Location](IStreamPropertiesProducer.Location.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.Location') | Specify location of the stream in data catalogue.<br/>For example: /cars/ai/carA/. |
| [Metadata](IStreamPropertiesProducer.Metadata.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.Metadata') | Metadata of the stream. |
| [Name](IStreamPropertiesProducer.Name.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.Name') | Name of the stream. |
| [Parents](IStreamPropertiesProducer.Parents.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.Parents') | List of Stream Ids of the Parent streams |
| [TimeOfRecording](IStreamPropertiesProducer.TimeOfRecording.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.TimeOfRecording') | Date Time of stream recording. Commonly set to Datetime.UtcNow. |

| Methods | |
| :--- | :--- |
| [AddParent(string)](IStreamPropertiesProducer.AddParent(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.AddParent(string)') | Adds a parent stream. |
| [Flush()](IStreamPropertiesProducer.Flush().md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.Flush()') | Immediately writes the properties yet to be sent instead of waiting for the flush timer (20ms) |
| [RemoveParent(string)](IStreamPropertiesProducer.RemoveParent(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamPropertiesProducer.RemoveParent(string)') | Removes a parent stream |
