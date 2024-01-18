#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models.StreamProducer](QuixStreams.Streaming.Models.StreamProducer.md 'QuixStreams.Streaming.Models.StreamProducer').[StreamTimeseriesProducerExtensions](StreamTimeseriesProducerExtensions.md 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions')

## StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(this IStreamTimeseriesProducer, int) Method

Creates a new [LeadingEdgeTimeBuffer](LeadingEdgeTimeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeTimeBuffer') using the [producer](StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisIStreamTimeseriesProducer,int).md#QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer 'QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer, int).producer') where tags do not  
form part of the row's key and can be freely modified after initial values

```csharp
public static QuixStreams.Streaming.Models.LeadingEdgeTimeBuffer CreateLeadingEdgeTimeBuffer(this QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer producer, int leadingEdgeDelayMs);
```
#### Parameters

<a name='QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).producer'></a>

`producer` [IStreamTimeseriesProducer](IStreamTimeseriesProducer.md 'QuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer')

The timeseries stream producer

<a name='QuixStreams.Streaming.Models.StreamProducer.StreamTimeseriesProducerExtensions.CreateLeadingEdgeTimeBuffer(thisQuixStreams.Streaming.Models.StreamProducer.IStreamTimeseriesProducer,int).leadingEdgeDelayMs'></a>

`leadingEdgeDelayMs` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

Leading edge delay configuration in Milliseconds

#### Returns
[LeadingEdgeTimeBuffer](LeadingEdgeTimeBuffer.md 'QuixStreams.Streaming.Models.LeadingEdgeTimeBuffer')