#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.States](QuixStreams.Streaming.States.md 'QuixStreams.Streaming.States').[StreamStateManager](StreamStateManager.md 'QuixStreams.Streaming.States.StreamStateManager')

## StreamStateManager.GetOrCreate(ITopicConsumer, StreamConsumerId, ILoggerFactory) Method

Initializes or gets an existing instance of the [StreamStateManager](StreamStateManager.md 'QuixStreams.Streaming.States.StreamStateManager') class with the specified parameters.

```csharp
public static QuixStreams.Streaming.States.StreamStateManager GetOrCreate(QuixStreams.Streaming.ITopicConsumer topicConsumer, QuixStreams.Streaming.Models.StreamConsumerId streamConsumerId, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory);
```
#### Parameters

<a name='QuixStreams.Streaming.States.StreamStateManager.GetOrCreate(QuixStreams.Streaming.ITopicConsumer,QuixStreams.Streaming.Models.StreamConsumerId,Microsoft.Extensions.Logging.ILoggerFactory).topicConsumer'></a>

`topicConsumer` [ITopicConsumer](ITopicConsumer.md 'QuixStreams.Streaming.ITopicConsumer')

The topic consumer used for committing state changes.

<a name='QuixStreams.Streaming.States.StreamStateManager.GetOrCreate(QuixStreams.Streaming.ITopicConsumer,QuixStreams.Streaming.Models.StreamConsumerId,Microsoft.Extensions.Logging.ILoggerFactory).streamConsumerId'></a>

`streamConsumerId` [StreamConsumerId](StreamConsumerId.md 'QuixStreams.Streaming.Models.StreamConsumerId')

Stream consumer identifier information.

<a name='QuixStreams.Streaming.States.StreamStateManager.GetOrCreate(QuixStreams.Streaming.ITopicConsumer,QuixStreams.Streaming.Models.StreamConsumerId,Microsoft.Extensions.Logging.ILoggerFactory).loggerFactory'></a>

`loggerFactory` [Microsoft.Extensions.Logging.ILoggerFactory](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.Extensions.Logging.ILoggerFactory 'Microsoft.Extensions.Logging.ILoggerFactory')

The logger factory used for creating loggers.

#### Returns
[StreamStateManager](StreamStateManager.md 'QuixStreams.Streaming.States.StreamStateManager')