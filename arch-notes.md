## Library architecture notes

The C# Quix Streams library is organized in 3 main layers:

```

   ┌───────────────────────────┐
   │      Streaming layer      │    /CSharp/QuixStreams.Streaming
   └─────────────┬─────────────┘
                 │
                 │
   ┌─────────────▼─────────────┐
   │      Telemetry layer      │    /CSharp/QuixStreams.Telemetry
   └─────────────┬─────────────┘
                 │
                 │
   ┌─────────────▼─────────────┐
   │   Kafka Transport layer   │    /CSharp/QuixStreams.Kafka.Transport
   ├───────────────────────────┤
   │    Kafka wrapper layer    │    /CSharp/QuixStreams.Kafka
   └───────────────────────────┘

```

Each layer has his own responsibilities:
 
- <b>Streaming layer</b>: This is the user facing layer of the library. It includes all the <b>syntax sugar</b> needed to have a pleasant experience with the library.

- <b>Telemetry layer</b>: This layer implements the `Codecs` to ser/des the <b>Telemetry messages</b> of Quix Streams protocol. This includes time-series and non time-series messages, stream metadata, stream properties messages, parameters definitions, as well as creating the [Stream context](#library-features-) scopes. The layer also implements a `Stream Pipeline` system to concatenate different components that can be used to implement complex low-level Telemetry services.

- <b>Transport layer</b>: This layer is responsible for handling <b>communication with the kafka broker</b>. It introduces additional features to ease the use of Kafka, improves on some of the broker's limitations and contains workarounds to some known issues. The library relies on [Confluent .NET Client for Apache Kafka](https://github.com/confluentinc/confluent-kafka-dotnet).

For more information and general questions about the architecture of the library you can join to our official [Slack channel](https://quix.io/slack-invite).