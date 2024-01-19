#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Raw](QuixStreams.Streaming.Raw.md 'QuixStreams.Streaming.Raw').[RawTopicProducer](RawTopicProducer.md 'QuixStreams.Streaming.Raw.RawTopicProducer')

## RawTopicProducer(IKafkaProducer, string) Constructor

Initializes a new instance of [RawTopicProducer](RawTopicProducer.md 'QuixStreams.Streaming.Raw.RawTopicProducer')

```csharp
public RawTopicProducer(QuixStreams.Kafka.IKafkaProducer kafkaProducer, string topicName=null);
```
#### Parameters

<a name='QuixStreams.Streaming.Raw.RawTopicProducer.RawTopicProducer(QuixStreams.Kafka.IKafkaProducer,string).kafkaProducer'></a>

`kafkaProducer` [QuixStreams.Kafka.IKafkaProducer](https://docs.microsoft.com/en-us/dotnet/api/QuixStreams.Kafka.IKafkaProducer 'QuixStreams.Kafka.IKafkaProducer')

The kafka producer to use

<a name='QuixStreams.Streaming.Raw.RawTopicProducer.RawTopicProducer(QuixStreams.Kafka.IKafkaProducer,string).topicName'></a>

`topicName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The optional topic name to use