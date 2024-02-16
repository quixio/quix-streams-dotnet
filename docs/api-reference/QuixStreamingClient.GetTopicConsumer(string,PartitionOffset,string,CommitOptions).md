#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[QuixStreamingClient](QuixStreamingClient.md 'QuixStreams.Streaming.QuixStreamingClient')

## QuixStreamingClient.GetTopicConsumer(string, PartitionOffset, string, CommitOptions) Method

Gets a topic consumer capable of subscribing to receive incoming streams.

```csharp
public QuixStreams.Streaming.ITopicConsumer GetTopicConsumer(string topicIdOrName, QuixStreams.Kafka.PartitionOffset partitionOffset, string consumerGroup=null, QuixStreams.Kafka.Transport.CommitOptions options=null);
```
#### Parameters

<a name='QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string,QuixStreams.Kafka.PartitionOffset,string,QuixStreams.Kafka.Transport.CommitOptions).topicIdOrName'></a>

`topicIdOrName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order

<a name='QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string,QuixStreams.Kafka.PartitionOffset,string,QuixStreams.Kafka.Transport.CommitOptions).partitionOffset'></a>

`partitionOffset` [QuixStreams.Kafka.PartitionOffset](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Kafka.PartitionOffset 'QuixStreams.Kafka.PartitionOffset')

The partition offset to start reading from

<a name='QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string,QuixStreams.Kafka.PartitionOffset,string,QuixStreams.Kafka.Transport.CommitOptions).consumerGroup'></a>

`consumerGroup` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.

<a name='QuixStreams.Streaming.QuixStreamingClient.GetTopicConsumer(string,QuixStreams.Kafka.PartitionOffset,string,QuixStreams.Kafka.Transport.CommitOptions).options'></a>

`options` [QuixStreams.Kafka.Transport.CommitOptions](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Kafka.Transport.CommitOptions 'QuixStreams.Kafka.Transport.CommitOptions')

The settings to use for committing

Implements [GetTopicConsumer(string, PartitionOffset, string, CommitOptions)](IQuixStreamingClient.GetTopicConsumer(string,PartitionOffset,string,CommitOptions).md 'QuixStreams.Streaming.IQuixStreamingClient.GetTopicConsumer(string, QuixStreams.Kafka.PartitionOffset, string, QuixStreams.Kafka.Transport.CommitOptions)')

#### Returns
[ITopicConsumer](ITopicConsumer.md 'QuixStreams.Streaming.ITopicConsumer')  
Instance of [ITopicConsumer](ITopicConsumer.md 'QuixStreams.Streaming.ITopicConsumer')