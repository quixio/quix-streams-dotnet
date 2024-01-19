#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[IQuixStreamingClientAsync](IQuixStreamingClientAsync.md 'QuixStreams.Streaming.IQuixStreamingClientAsync')

## IQuixStreamingClientAsync.GetRawTopicProducerAsync(string) Method

Asynchronously gets a topic producer capable of publishing non-quixstreams messages.

```csharp
System.Threading.Tasks.Task<QuixStreams.Streaming.Raw.IRawTopicProducer> GetRawTopicProducerAsync(string topicIdOrName);
```
#### Parameters

<a name='QuixStreams.Streaming.IQuixStreamingClientAsync.GetRawTopicProducerAsync(string).topicIdOrName'></a>

`topicIdOrName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order

#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')[IRawTopicProducer](IRawTopicProducer.md 'QuixStreams.Streaming.Raw.IRawTopicProducer')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')  
A task returning an instance of [IRawTopicProducer](IRawTopicProducer.md 'QuixStreams.Streaming.Raw.IRawTopicProducer')