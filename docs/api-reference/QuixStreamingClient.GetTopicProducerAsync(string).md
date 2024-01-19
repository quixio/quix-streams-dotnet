#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[QuixStreamingClient](QuixStreamingClient.md 'QuixStreams.Streaming.QuixStreamingClient')

## QuixStreamingClient.GetTopicProducerAsync(string) Method

Asynchronously gets a topic producer capable of publishing stream messages.

```csharp
public System.Threading.Tasks.Task<QuixStreams.Streaming.ITopicProducer> GetTopicProducerAsync(string topicIdOrName);
```
#### Parameters

<a name='QuixStreams.Streaming.QuixStreamingClient.GetTopicProducerAsync(string).topicIdOrName'></a>

`topicIdOrName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

Id or name of the topic. If name is provided, workspace will be derived from environment variable or token, in that order

Implements [GetTopicProducerAsync(string)](IQuixStreamingClientAsync.GetTopicProducerAsync(string).md 'QuixStreams.Streaming.IQuixStreamingClientAsync.GetTopicProducerAsync(string)')

#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')[ITopicProducer](ITopicProducer.md 'QuixStreams.Streaming.ITopicProducer')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task`1')  
A task returning an instance of [ITopicProducer](ITopicProducer.md 'QuixStreams.Streaming.ITopicProducer')