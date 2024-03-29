#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[StreamTimeseriesProducer](StreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducer')

## StreamTimeseriesProducer.DefaultLocation Property

Default Location of the parameters. Parameter definitions added with [AddDefinition(string, string, string)](IStreamTimeseriesProducer.AddDefinition(string,string,string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddDefinition(string, string, string)') will be inserted at this location.  
See [AddLocation(string)](IStreamTimeseriesProducer.AddLocation(string).md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.AddLocation(string)') for adding definitions at a different location without changing default.  
Example: "/Group1/SubGroup2"

```csharp
public string DefaultLocation { get; set; }
```

Implements [DefaultLocation](IStreamTimeseriesProducer.DefaultLocation.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer.DefaultLocation')

#### Property Value
[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')