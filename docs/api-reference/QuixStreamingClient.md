#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming')

## QuixStreamingClient Class

Streaming client for Kafka configured automatically using Environment Variables and Quix platform endpoints.  
Use this Client when you use this library together with Quix platform.

```csharp
public class QuixStreamingClient :
QuixStreams.Streaming.IQuixStreamingClient,
QuixStreams.Streaming.IQuixStreamingClientAsync
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; QuixStreamingClient

Implements [IQuixStreamingClient](IQuixStreamingClient.md 'QuixStreams.Streaming.IQuixStreamingClient'), [IQuixStreamingClientAsync](IQuixStreamingClientAsync.md 'QuixStreams.Streaming.IQuixStreamingClientAsync')

| Constructors | |
| :--- | :--- |
| [QuixStreamingClient(string, bool, IDictionary&lt;string,string&gt;, bool, HttpClient, string, Uri)](QuixStreamingClient.QuixStreamingClient(string,bool,IDictionary_string,string_,bool,HttpClient,string,Uri).md 'QuixStreams.Streaming.QuixStreamingClient.QuixStreamingClient(string, bool, System.Collections.Generic.IDictionary<string,string>, bool, System.Net.Http.HttpClient, string, System.Uri)') | Initializes a new instance of [KafkaStreamingClient](KafkaStreamingClient.md 'QuixStreams.Streaming.KafkaStreamingClient') that is capable of creating topic consumer and producers |

| Fields | |
| :--- | :--- |
| [ApiUrl](QuixStreamingClient.ApiUrl.md 'QuixStreams.Streaming.QuixStreamingClient.ApiUrl') | The base API uri. Defaults to `https://portal-api.platform.quix.io`, or environment variable `Quix__Portal__Api` if available. |
| [CachePeriod](QuixStreamingClient.CachePeriod.md 'QuixStreams.Streaming.QuixStreamingClient.CachePeriod') | The period for which some API responses will be cached to avoid excessive amount of calls. Defaults to 1 minute. |

| Properties | |
| :--- | :--- |
| [TokenValidationConfig](QuixStreamingClient.TokenValidationConfig.md 'QuixStreams.Streaming.QuixStreamingClient.TokenValidationConfig') | Gets or sets the token validation configuration |

| Methods | |
| :--- | :--- |
| [GetRawTopicConsumer(string, string, Nullable&lt;AutoOffsetReset&gt;)](QuixStreamingClient.GetRawTopicConsumer(string,string,Nullable_AutoOffsetReset_).md 'QuixStreams.Streaming.QuixStreamingClient.GetRawTopicConsumer(string, string, System.Nullable<QuixStreams.Telemetry.Kafka.AutoOffsetReset>)') | Gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages. |
| [GetRawTopicConsumerAsync(string, string, Nullable&lt;AutoOffsetReset&gt;)](QuixStreamingClient.GetRawTopicConsumerAsync(string,string,Nullable_AutoOffsetReset_).md 'QuixStreams.Streaming.QuixStreamingClient.GetRawTopicConsumerAsync(string, string, System.Nullable<QuixStreams.Telemetry.Kafka.AutoOffsetReset>)') | Asynchronously gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages. |
| [GetRawTopicProducer(string)](QuixStreamingClient.GetRawTopicProducer(string).md 'QuixStreams.Streaming.QuixStreamingClient.GetRawTopicProducer(string)') | Gets a topic producer capable of publishing non-quixstreams messages. |
| [GetRawTopicProducerAsync(string)](QuixStreamingClient.GetRawTopicProducerAsync(string).md 'QuixStreams.Streaming.QuixStreamingClient.GetRawTopicProducerAsync(string)') | Asynchronously gets a topic producer capable of publishing non-quixstreams messages. |
| [GetTopicConsumer(string, PartitionOffset, string, CommitOptions)](QuixStreamingClient.GetTopicConsumer(string,PartitionOffset,string,CommitOptions).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string, QuixStreams.Kafka.PartitionOffset, string, QuixStreams.Kafka.Transport.CommitOptions)') | Gets a topic consumer capable of subscribing to receive incoming streams. |
| [GetTopicConsumer(string, string, CommitOptions, AutoOffsetReset)](QuixStreamingClient.GetTopicConsumer(string,string,CommitOptions,AutoOffsetReset).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string, string, QuixStreams.Kafka.Transport.CommitOptions, QuixStreams.Telemetry.Kafka.AutoOffsetReset)') | Gets a topic consumer capable of subscribing to receive incoming streams. |
| [GetTopicConsumerAsync(string, PartitionOffset, string, CommitOptions)](QuixStreamingClient.GetTopicConsumerAsync(string,PartitionOffset,string,CommitOptions).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumerAsync(string, QuixStreams.Kafka.PartitionOffset, string, QuixStreams.Kafka.Transport.CommitOptions)') | Asynchronously gets a topic consumer capable of subscribing to receive incoming streams. |
| [GetTopicConsumerAsync(string, string, CommitOptions, AutoOffsetReset)](QuixStreamingClient.GetTopicConsumerAsync(string,string,CommitOptions,AutoOffsetReset).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumerAsync(string, string, QuixStreams.Kafka.Transport.CommitOptions, QuixStreams.Telemetry.Kafka.AutoOffsetReset)') | Asynchronously gets a topic consumer capable of subscribing to receive incoming streams. |
| [GetTopicProducer(string)](QuixStreamingClient.GetTopicProducer(string).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicProducer(string)') | Gets a topic producer capable of publishing stream messages. |
| [GetTopicProducerAsync(string)](QuixStreamingClient.GetTopicProducerAsync(string).md 'QuixStreams.Streaming.QuixStreamingClient.GetTopicProducerAsync(string)') | Asynchronously gets a topic producer capable of publishing stream messages. |
