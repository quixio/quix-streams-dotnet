#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming.States](QuixStreams.Streaming.States.md 'QuixStreams.Streaming.States').[StreamStateManager](StreamStateManager.md 'QuixStreams.Streaming.States.StreamStateManager')

## StreamStateManager.GetScalarState<T>(string, StreamStateScalarDefaultValueDelegate<T>) Method

Creates a new application state of scalar type with automatically managed lifecycle for the stream

```csharp
public QuixStreams.Streaming.States.StreamScalarState<T> GetScalarState<T>(string stateName, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T> defaultValueFactory=null);
```
#### Type parameters

<a name='QuixStreams.Streaming.States.StreamStateManager.GetScalarState_T_(string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T'></a>

`T`
#### Parameters

<a name='QuixStreams.Streaming.States.StreamStateManager.GetScalarState_T_(string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).stateName'></a>

`stateName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The name of the state

<a name='QuixStreams.Streaming.States.StreamStateManager.GetScalarState_T_(string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).defaultValueFactory'></a>

`defaultValueFactory` [QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate&lt;](StreamStateScalarDefaultValueDelegate_T_().md 'QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>()')[T](StreamStateManager.GetScalarState_T_(string,StreamStateScalarDefaultValueDelegate_T_).md#QuixStreams.Streaming.States.StreamStateManager.GetScalarState_T_(string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T 'QuixStreams.Streaming.States.StreamStateManager.GetScalarState<T>(string, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>).T')[&gt;](StreamStateScalarDefaultValueDelegate_T_().md 'QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>()')

The value factory for the state when the state has no value

#### Returns
[QuixStreams.Streaming.States.StreamScalarState&lt;](StreamScalarState_T_.md 'QuixStreams.Streaming.States.StreamScalarState<T>')[T](StreamStateManager.GetScalarState_T_(string,StreamStateScalarDefaultValueDelegate_T_).md#QuixStreams.Streaming.States.StreamStateManager.GetScalarState_T_(string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T 'QuixStreams.Streaming.States.StreamStateManager.GetScalarState<T>(string, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>).T')[&gt;](StreamScalarState_T_.md 'QuixStreams.Streaming.States.StreamScalarState<T>')  
Dictionary stream state