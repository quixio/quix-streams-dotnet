#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[StreamTimeseriesProducerExtensions](StreamTimeseriesProducerExtensions.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions')

## StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(this IStreamTimeseriesProducer, int) Method

Creates a new [LeadingEdgeBuffer](LeadingEdgeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeBuffer') using the [producer](StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisIStreamTimeseriesProducer,int).md#QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int).producer') where tags form part of  
the row's key and can't be modified after initial values

```csharp
public static QuixStreams.Streaming.Models.LeadingEdgeBuffer CreateLeadingEdgeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer producer, int leadingEdgeDelayMs);
```
#### Parameters

<a name='QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer'></a>

`producer` [IStreamTimeseriesProducer](IStreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer')

The timeseries stream producer

<a name='QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).leadingEdgeDelayMs'></a>

`leadingEdgeDelayMs` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

Leading edge delay configuration in Milliseconds

#### Returns
[LeadingEdgeBuffer](LeadingEdgeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeBuffer')