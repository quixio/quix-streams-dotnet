#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming')

## IQuixStreamingClientAsync Interface

Represents an asynchronous streaming client for Kafka configured automatically using Environment Variables and Quix platform endpoints.  
Use this Client when you use this library together with Quix platform.

```csharp
public interface IQuixStreamingClientAsync
```

Derived  
&#8627; [IQuixStreamingClient](IQuixStreamingClient.md 'QuixStreams.Streaming.IQuixStreamingClient')  
&#8627; [QuixStreamingClient](QuixStreamingClient.md 'QuixStreams.Streaming.QuixStreamingClient')

| Methods | |
| :--- | :--- |
| [GetRawTopicConsumerAsync(string, string, Nullable&lt;AutoOffsetReset&gt;)](IQuixStreamingClientAsync.GetRawTopicConsumerAsync(string,string,Nullable_AutoOffsetReset_).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetRawTopicConsumerAsync(string, string, System.Nullable<QuixStreams.Telemetry.Kafka.AutoOffsetReset>)') | Asynchronously gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages. |
| [GetRawTopicProducerAsync(string)](IQuixStreamingClientAsync.GetRawTopicProducerAsync(string).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetRawTopicProducerAsync(string)') | Asynchronously gets a topic producer capable of publishing non-quixstreams messages. |
| [GetTopicConsumerAsync(string, string, CommitOptions, AutoOffsetReset)](IQuixStreamingClientAsync.GetTopicConsumerAsync(string,string,CommitOptions,AutoOffsetReset).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetTopicConsumerAsync(string, string, QuixStreams.Kafka.Transport.CommitOptions, QuixStreams.Telemetry.Kafka.AutoOffsetReset)') | Asynchronously gets a topic consumer capable of subscribing to receive incoming streams. |
| [GetTopicProducerAsync(string)](IQuixStreamingClientAsync.GetTopicProducerAsync(string).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetTopicProducerAsync(string)') | Asynchronously gets a topic producer capable of publishing stream messages. |
