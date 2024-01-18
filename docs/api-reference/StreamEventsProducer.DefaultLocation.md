#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[StreamEventsProducer](StreamEventsProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamEventsProducer')

## StreamEventsProducer.DefaultLocation Property

Default Location of the events. Event definitions added with [AddDefinition(string, string, string)](IStreamEventsProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddDefinition(string, string, string)') will be inserted at this location.  
See [AddLocation(string)](IStreamEventsProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.AddLocation(string)') for adding definitions at a different location without changing default.  
Example: "/Group1/SubGroup2"

```csharp
public string DefaultLocation { get; set; }
```

Implements [DefaultLocation](IStreamEventsProducer.DefaultLocation.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamEventsProducer.DefaultLocation')

#### Property Value
[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')