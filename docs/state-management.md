# State management

Your code may get restarted multiple times. A user intervention (like manually stopping and starting) or runtime error could cause your application to terminate. 

!!! note

	When using Quix Cloud, the platform automatically detects the problem and restarts the underlying service in an attempt to recover from the fault.

Due to the code being run in memory, each time a deployment restarts, internal variables will be reset. For example, if you were to calculate the count of the elements in the stream, this counter would get reset on each restart. The counter would then start at the default value not knowing what was the last known value in the state of the previous run before program terminated.

Quix Streams has state management built in to enable values to be used and persisted across restarts of a given deployment. Quix Streams has options to persists the state using RocksDB or to keep in memory.

!!! note

	When using Quix Cloud, the platform provides your replicas with a shared state store when enabled.

The library provides automatic state management which handles application lifecycle automatically, such as commits and revocation to ensure the state reflects the processed and committed messages only. There are two types of state available: Dictionary state and Scalar state. The stream state management is available on the stream consumer and not the producer currently.

## Reading and writing


``` csharp
// ... context where stream consumer is available such as OnStreamReceived, OnDataReceived handlers ...

// This will return a state where type is 'int'
var myIntState = streamConsumer.GetDictionaryState<int>("RollingSum");   
myIntState["my_key"] = 42
myIntState["my_key"] += 13

// this will return a state where type is a specific dictionary type, with default value
var myLastValueState = streamConsumer.GetDictionaryState("LastValue", (missingKey) => new Dictionary<string, double>());
myLastValueState["someParam"]["RollingAverage"] = 37.872
myLastValueState["someParam"]["LastValue"] = 6
myLastValueState["someParam"]["Mean"] = 37
```

## Querying

You can query the existing states several ways through StreamStateManager class. You can access it in several ways through a topic or stream consumer.

``` csharp
// From topic consumer
var manager = topicConsumer.GetStreamStateManager("my_stream_id");
var streamState = manager.GetDictionaryState<int>("some_state");
var streamStateValue = streamState["my_key"];

// Or from stream consumer
var manager = streamConsumer.GetStateManager();
// note, it also possible to directly use streamConsumer.GetDictionaryState<int>("some_state") instead, if other management APIs are not needed
var streamState = manager.GetDictionaryState<int>("some_state");
var streamStateValue = streamState["my_key"];
```

## Deleting

You can delete any or all state using the state manager. See [Querying](#querying) section for how to acquire the state manager class in different ways.

``` csharp
// when stream consumer is available
var streamStateManager = streamConsumer.GetStateManager();
streamStateManager.DeleteState("some_state");
streamStateManager.DeleteStates(); // deletes all
```

## Scalar state type
In addition to the dictionary state type, we also have the scalar state type. It functions similarly, but holds just a single value, making it simpler to use. Below is an example:

``` csharp
topicConsumer.OnStreamReceived += (sender, consumer) =>
{

    var totalRpm = consumer.GetStateManager().GetScalarState("total_rpm", (key) => 0d);

    consumer.Timeseries.OnDataReceived += (o, args) =>
    {
        foreach (var timestamp in args.Data.Timestamps)
        {
            var rpm = timestamp.Parameters["EngineRPM"].NumericValue;
            totalRpm.Value += rpm ?? 0;
            timestamp.AddValue("TotalEngineRPM", totalRpm.Value);
        }
    };
};
```

## Storage types

Any state storage is supported as long as as it implements IStateStorage. These are currently RocksDbStorage and InMemoryStorage.

The storage type must be specified at app level using the following code. The default option is RocksDbStorage.

``` csharp
var storage = new InMemoryStorage();
App.SetStateStorage(storage); // this mostly makes sense for testing until other storage types are implemented
```

## Using State storage directly

To use the library’s state management feature, create an instance of `RocksDbStorage` or `InMemoryStorage`, and then use the available methods on the instance to manipulate the state as needed. For example:

C\# supports two ways to call the Storage API.

    - Synchronous

    - Asynchronous ( methods are with Async suffix )

The Synchronous API. During a call to these synchronous methods, the
program thread execution is blocked.

``` csharp
var storage = new RocksDbStorage();

//clear storage ( remove all keys )
await storage.Clear();

//set value to specific key
await storage.Set("KEY1", 123);  //long
await storage.Set("KEY2", 1.23); //double
await storage.Set("KEY3", "1.23"); //string
await storage.Set("KEY4", new byte[]{12,53,23}); //binary
await storage.Set("KEY5", false); //boolean

//check if the key exists
await storage.ContainsKey("KEY1");

//retrieve value from key
await storage.GetLong("KEY1");
await storage.GetDouble("KEY2");
await storage.GetString("KEY3");
await storage.GetBinary("KEY4");
await storage.GetBinary("KEY5");

//list all keys in the storage
await storage.GetAllKeys();
```

The asynchronous API in which methods do contain Async suffix. These methods use the Task-Based Asynchronous Pattern (TAP) and returnTasks. TAP enables Quix to use async / await and avoid blocking the main thread on longer-running operations. In this case internal I/O.

``` csharp
var storage = new RocksDbStorage();

//clear storage ( remove all keys )
await storage.ClearAsync();

//set value to specific key
await storage.SetAsync("KEY1", 123);  //long
await storage.SetAsync("KEY2", 1.23); //double
await storage.SetAsync("KEY3", "1.23"); //string
await storage.SetAsync("KEY4", new byte[]{12,53,23}); //binary
await storage.SetAsync("KEY5", false); //boolean

//check if the key exists
await storage.ContainsKeyAsync("KEY1");

//retrieve value from key
await storage.GetLongAsync("KEY1");
await storage.GetDoubleAsync("KEY2");
await storage.GetStringAsync("KEY3");
await storage.GetBinaryAsync("KEY4");
await storage.GetBinaryAsync("KEY5");

//list all keys in the storage
await storage.GetAllKeysAsync();
```
    
## State folder

In Quix Cloud, when state management is enabled for a deployment, Quix Streams uses a `state` folder to store data and files. If running Quix Streams locally, the `state` folder is automatically created for you. You can read more about enabling state, and using the `state` folder, in the [state management documentation](https://quix.io/docs/platform/how-to/state-management.html).

Inside that folder, the structure goes like this:
```
{consumer_group}/{topic}/{partition_number}/
```
