#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming')

## App Class

Provides utilities to handle default streaming behaviors and automatic resource cleanup on shutdown.

```csharp
public static class App
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; App

| Methods | |
| :--- | :--- |
| [GetStateStorageRootDir()](App.GetStateStorageRootDir().md 'QuixStreams.Streaming.App.GetStateStorageRootDir()') | Retrieves the root directory for state storages |
| [Run(CancellationToken, Action, bool)](App.Run(CancellationToken,Action,bool).md 'QuixStreams.Streaming.App.Run(System.Threading.CancellationToken, System.Action, bool)') | Helper method to handle default streaming behaviors and handle automatic resource cleanup on shutdown |
| [SetStateStorageRootDir(string)](App.SetStateStorageRootDir(string).md 'QuixStreams.Streaming.App.SetStateStorageRootDir(string)') | Sets the state storage for the app |
| [SetStateStorageType(StateStorageTypes)](App.SetStateStorageType(StateStorageTypes).md 'QuixStreams.Streaming.App.SetStateStorageType(QuixStreams.State.Storage.StateStorageTypes)') | Sets the state storage for the app |
