#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer')

## StreamTimeseriesProducerExtensions Class

Provides extension methods for [IStreamProducer](IStreamProducer.md 'QuixStreams.Streaming.IStreamProducer').

```csharp
public static class StreamTimeseriesProducerExtensions
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; StreamTimeseriesProducerExtensions

| Methods | |
| :--- | :--- |
| [CreateLeadingEdgeBuffer(this IStreamTimeseriesProducer, int)](StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisIStreamTimeseriesProducer,int).md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int)') | Creates a new [LeadingEdgeBuffer](LeadingEdgeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeBuffer') using the [producer](StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisIStreamTimeseriesProducer,int).md#QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int).producer') where tags form part of<br/>the row's key and can't be modified after initial values |
| [CreateLeadingEdgeTimeBuffer(this IStreamTimeseriesProducer, int)](StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisIStreamTimeseriesProducer,int).md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int)') | Creates a new [LeadingEdgeTimeBuffer](LeadingEdgeTimeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeTimeBuffer') using the [producer](StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisIStreamTimeseriesProducer,int).md#QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int).producer') where tags do not<br/>form part of the row's key and can be freely modified after initial values |
