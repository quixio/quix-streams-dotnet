#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.States](QuixStreams.Streaming.States.md 'QuixStreams.Streaming.States')

## StreamStateManager Class

Manages the states.

```csharp
public class StreamStateManager
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; StreamStateManager

| Fields | |
| :--- | :--- |
| [StorageDir](StreamStateManager.StorageDir.md 'QuixStreams.Streaming.States.StreamStateManager.StorageDir') | The directory where the states are stored on disk. |

| Methods | |
| :--- | :--- |
| [DeleteState(string)](StreamStateManager.DeleteState(string).md 'QuixStreams.Streaming.States.StreamStateManager.DeleteState(string)') | Deletes the state with the specified name |
| [DeleteStates()](StreamStateManager.DeleteStates().md 'QuixStreams.Streaming.States.StreamStateManager.DeleteStates()') | Deletes all states for the current stream. |
| [GetDictionaryState(string)](StreamStateManager.GetDictionaryState(string).md 'QuixStreams.Streaming.States.StreamStateManager.GetDictionaryState(string)') | Creates a new application state of dictionary type with automatically managed lifecycle for the stream |
| [GetDictionaryState&lt;T&gt;(string, StreamStateDefaultValueDelegate&lt;T&gt;)](StreamStateManager.GetDictionaryState_T_(string,StreamStateDefaultValueDelegate_T_).md 'QuixStreams.Streaming.States.StreamStateManager.GetDictionaryState<T>(string, QuixStreams.Streaming.States.StreamStateDefaultValueDelegate<T>)') | Creates a new application state of dictionary type with automatically managed lifecycle for the stream |
| [GetOrCreate(ITopicConsumer, StreamConsumerId, ILoggerFactory)](StreamStateManager.GetOrCreate(ITopicConsumer,StreamConsumerId,ILoggerFactory).md 'QuixStreams.Streaming.States.StreamStateManager.GetOrCreate(QuixStreams.Streaming.ITopicConsumer, QuixStreams.Streaming.Models.StreamConsumerId, Microsoft.Extensions.Logging.ILoggerFactory)') | Initializes or gets an existing instance of the [StreamStateManager](StreamStateManager.md 'QuixStreams.Streaming.States.StreamStateManager') class with the specified parameters. |
| [GetScalarState(string)](StreamStateManager.GetScalarState(string).md 'QuixStreams.Streaming.States.StreamStateManager.GetScalarState(string)') | Creates a new application state of scalar type with automatically managed lifecycle for the stream |
| [GetScalarState&lt;T&gt;(string, StreamStateScalarDefaultValueDelegate&lt;T&gt;)](StreamStateManager.GetScalarState_T_(string,StreamStateScalarDefaultValueDelegate_T_).md 'QuixStreams.Streaming.States.StreamStateManager.GetScalarState<T>(string, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>)') | Creates a new application state of scalar type with automatically managed lifecycle for the stream |
| [TryRevoke(StreamConsumerId)](StreamStateManager.TryRevoke(StreamConsumerId).md 'QuixStreams.Streaming.States.StreamStateManager.TryRevoke(QuixStreams.Streaming.Models.StreamConsumerId)') | Tries to revoke the stream state manager for the specified stream consumer id. |
