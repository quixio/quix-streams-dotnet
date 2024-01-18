#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Raw](QuixStreams.Streaming.Raw.md 'QuixStreams.Streaming.Raw').[RawTopicConsumer](RawTopicConsumer.md 'QuixStreams.Streaming.Raw.RawTopicConsumer')

## RawTopicConsumer(IKafkaConsumer, string) Constructor

Initializes a new instance of [RawTopicConsumer](RawTopicConsumer.md 'QuixStreams.Streaming.Raw.RawTopicConsumer')

```csharp
public RawTopicConsumer(QuixStreams.Kafka.IKafkaConsumer kafkaConsumer, string topicName=null);
```
#### Parameters

<a name='QuixStreams.Streaming.Raw.RawTopicConsumer.RawTopicConsumer(QuixStreams.Kafka.IKafkaConsumer,string).kafkaConsumer'></a>

`kafkaConsumer` [QuixStreams.Kafka.IKafkaConsumer](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Kafka.IKafkaConsumer 'QuixStreams.Kafka.IKafkaConsumer')

The kafka consumer to use

<a name='QuixStreams.Streaming.Raw.RawTopicConsumer.RawTopicConsumer(QuixStreams.Kafka.IKafkaConsumer,string).topicName'></a>

`topicName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The optional topic name to use