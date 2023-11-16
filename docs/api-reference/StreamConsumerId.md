#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.Models](QuixStreams.Streaming.Models.md 'QuixStreams.Streaming.Models')

## StreamConsumerId Class

```csharp
public class StreamConsumerId
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; StreamConsumerId

| Constructors | |
| :--- | :--- |
| [StreamConsumerId(string, string, int, string)](StreamConsumerId.StreamConsumerId(string,string,int,string).md 'QuixStreams.Streaming.Models.StreamConsumerId.StreamConsumerId(string, string, int, string)') | Represents a unique identifier for a stream consumer.<br/><param name="consumerGroup">Name of the consumer group.</param><param name="topicName">Name of the topic.</param><param name="partition">Topic partition number.</param><param name="streamId">Stream Id of the source that has generated this Stream Consumer.</param><br/>Commonly the Stream Id will be coming from the protocol. <br/>If no stream Id is passed, like when a new stream is created for producing data, a Guid is generated automatically. |
