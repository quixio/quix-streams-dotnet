#### [QuixStreams.Streaming](index.md 'index')
### [QuixStreams.Streaming](QuixStreams.Streaming.md 'QuixStreams.Streaming').[IStreamConsumerExtensions](IStreamConsumerExtensions.md 'QuixStreams.Streaming.IStreamConsumerExtensions')

## IStreamConsumerExtensions.GetScalarState<T>(this IStreamConsumer, string, StreamStateScalarDefaultValueDelegate<T>) Method

Gets the scalar type stream state for the specified storage name using the provided default value factory.

```csharp
public static QuixStreams.Streaming.States.StreamScalarState<T> GetScalarState<T>(this QuixStreams.Streaming.IStreamConsumer streamConsumer, string stateName, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T> defaultValueFactory=null);
```
#### Type parameters

<a name='QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T'></a>

`T`

The type of the stream state value.
#### Parameters

<a name='QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).streamConsumer'></a>

`streamConsumer` [IStreamConsumer](IStreamConsumer.md 'QuixStreams.Streaming.IStreamConsumer')

The stream consumer to get the state for

<a name='QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).stateName'></a>

`stateName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

The name of the state.

<a name='QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).defaultValueFactory'></a>

`defaultValueFactory` [QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate&lt;](StreamStateScalarDefaultValueDelegate_T_().md 'QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>()')[T](IStreamConsumerExtensions.GetScalarState_T_(thisIStreamConsumer,string,StreamStateScalarDefaultValueDelegate_T_).md#QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T 'QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState<T>(this QuixStreams.Streaming.IStreamConsumer, string, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>).T')[&gt;](StreamStateScalarDefaultValueDelegate_T_().md 'QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>()')

A delegate that creates the default value for the stream state.

#### Returns
[QuixStreams.Streaming.States.StreamScalarState&lt;](StreamScalarState_T_.md 'QuixStreams.Streaming.States.StreamScalarState<T>')[T](IStreamConsumerExtensions.GetScalarState_T_(thisIStreamConsumer,string,StreamStateScalarDefaultValueDelegate_T_).md#QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState_T_(thisQuixStreams.Streaming.IStreamConsumer,string,QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate_T_).T 'QuixStreams.Streaming.IStreamConsumerExtensions.GetScalarState<T>(this QuixStreams.Streaming.IStreamConsumer, string, QuixStreams.Streaming.States.StreamStateScalarDefaultValueDelegate<T>).T')[&gt;](StreamScalarState_T_.md 'QuixStreams.Streaming.States.StreamScalarState<T>')  
The dictionary stream state for the specified storage name using the provided default value factory.