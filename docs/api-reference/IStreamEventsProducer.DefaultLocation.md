#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[IStreamEventsProducer](IStreamEventsProducer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer')

## IStreamEventsProducer.DefaultLocation Property

Default Location of the events. Event definitions added with [AddDefinition(string, string, string)](IStreamEventsProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddDefinition(string, string, string)') will be inserted at this location.  
See [AddLocation(string)](IStreamEventsProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddLocation(string)') for adding definitions at a different location without changing default.  
Example: "/Group1/SubGroup2"

```csharp
string DefaultLocation { get; set; }
```

#### Property Value
[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')