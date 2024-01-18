#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[QuixStreamingClient](QuixStreamingClient.md 'QuixStreams.Streaming.QuixStreamingClient')

## QuixStreamingClient.GetRawTopicConsumerAsync(string, string, Nullable<AutoOffsetReset>) Method

Asynchronously gets a topic consumer capable of subscribing to receive non-quixstreams incoming messages.

```csharp
public System.Threading.Tasks.Task<QuixStreams.Streaming.Raw.IRawTopicConsumer> GetRawTopicConsumerAsync(string topicIdOrName, string consumerGroup=null, System.Nullable<QuixStreams.Telemetry.Kafka.AutoOffsetReset> autoOffset=null);
```
#### Parameters

<a name='QuixStreams.Streaming.QuixStreamingClient.GetRawTopicConsumerAsync(string,string,System.Nullable_QuixStreams.Telemetry.Kafka.AutoOffsetReset_).topicIdOrName'></a>

`topicIdOrName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order

<a name='QuixStreams.Streaming.QuixStreamingClient.GetRawTopicConsumerAsync(string,string,System.Nullable_QuixStreams.Telemetry.Kafka.AutoOffsetReset_).consumerGroup'></a>

`consumerGroup` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.

<a name='QuixStreams.Streaming.QuixStreamingClient.GetRawTopicConsumerAsync(string,string,System.Nullable_QuixStreams.Telemetry.Kafka.AutoOffsetReset_).autoOffset'></a>

`autoOffset` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[QuixStreams.Telemetry.Kafka.AutoOffsetReset](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Telemetry.Kafka.AutoOffsetReset 'QuixStreams.Telemetry.Kafka.AutoOffsetReset')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

The offset to use when there is no saved offset for the consumer group.

Implements [GetRawTopicConsumerAsync(string, string, Nullable&lt;AutoOffsetReset&gt;)](IQuixStreamingClientAsync.GetRawTopicConsumerAsync(string,string,Nullable_AutoOffsetReset_).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetRawTopicConsumerAsync(string, string, System.Nullable<QuixStreams.Telemetry.Kafka.AutoOffsetReset>)')

#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')[IRawTopicConsumer](IRawTopicConsumer.md 'QuixStreams.Streaming.Raw.IRawTopicConsumer')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')  
A task returning an instance of [IRawTopicConsumer](IRawTopicConsumer.md 'QuixStreams.Streaming.Raw.IRawTopicConsumer')