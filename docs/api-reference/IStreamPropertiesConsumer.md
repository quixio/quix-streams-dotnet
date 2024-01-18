#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamConsumer](QuixStreams.Streaming.Models.StreamConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer')

## IStreamPropertiesConsumer Interface

Represents properties and metadata of the stream.  
All changes to these properties are automatically populated to this class.

```csharp
public interface IStreamPropertiesConsumer :
System.IDisposable
```

Derived  
&#8627; [StreamPropertiesConsumer](StreamPropertiesConsumer.md 'QuixStreams.Streaming.Models.StreamConsumer.StreamPropertiesConsumer')

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')

| Properties | |
| :--- | :--- |
| [Location](IStreamPropertiesConsumer.Location.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.Location') | Gets the location of the stream |
| [Metadata](IStreamPropertiesConsumer.Metadata.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.Metadata') | Gets the metadata of the stream |
| [Name](IStreamPropertiesConsumer.Name.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.Name') | Gets the name of the stream |
| [Parents](IStreamPropertiesConsumer.Parents.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.Parents') | Gets the list of Stream IDs for the parent streams |
| [TimeOfRecording](IStreamPropertiesConsumer.TimeOfRecording.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.TimeOfRecording') | Gets the datetime of the recording |

| Events | |
| :--- | :--- |
| [OnChanged](IStreamPropertiesConsumer.OnChanged.md 'QuixStreams.Streaming.Models.StreamConsumer.IStreamPropertiesConsumer.OnChanged') | Raised when the stream properties change |
