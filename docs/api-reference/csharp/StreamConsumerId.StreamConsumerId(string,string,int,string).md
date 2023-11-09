#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models](QuixStreams.Streaming.Models.md 'QuixStreams.Streaming.Models').[StreamConsumerId](StreamConsumerId.md 'QuixStreams.Streaming.Models.StreamConsumerId')

## StreamConsumerId(string, string, int, string) Constructor

Represents a unique identifier for a stream consumer.  
<param name="consumerGroup">Name of the consumer group.</param><param name="topicName">Name of the topic.</param><param name="partition">Topic partition number.</param><param name="streamId">Stream Id of the source that has generated this Stream Consumer.</param>  
Commonly the Stream Id will be coming from the protocol.   
If no stream Id is passed, like when a new stream is created for producing data, a Guid is generated automatically.

```csharp
public StreamConsumerId(string consumerGroup, string topicName, int partition, string streamId);
```
#### Parameters

<a name='QuixStreams.Streaming.Models.StreamConsumerId.StreamConsumerId(string,string,int,string).consumerGroup'></a>

`consumerGroup` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='QuixStreams.Streaming.Models.StreamConsumerId.StreamConsumerId(string,string,int,string).topicName'></a>

`topicName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='QuixStreams.Streaming.Models.StreamConsumerId.StreamConsumerId(string,string,int,string).partition'></a>

`partition` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='QuixStreams.Streaming.Models.StreamConsumerId.StreamConsumerId(string,string,int,string).streamId'></a>

`streamId` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')