# Migrating from previous versions

Our goal is to minimize the occurrence of breaking changes, and if we do need to make them, we'll do so simultaneously. In cases where such changes are necessary, we'll provide migration steps from previous versions to assist in the transition. To prevent undue verbosity, we'll only show one difference unless it's language-specific, such as naming conventions (casing vs underscore).

## 0.4.* -> 0.5.0

### The library is renamed

The packages will be available under `QuixStreams.*` rather than `Quix.Sdk.*`. The latter also resulted in namespace changes.

### Library availability

Previously, the library was not open source and was distributed via our public feed.

As of 0.5.0 it is distributed via [nuget](https://www.nuget.org/packages/QuixStreams.Streaming).

### StreamingClient renamed to KafkaStreamingClient

We renamed the `StreamingClient` to be more specific to the technology it works with.

### Readers and Writers renamed to Consumers and Producers

The modifications will have the most significant impact code that subscribes events using a method with a particular signature rather than a lambda expression. The alterations are as follows:

- `StreamReader|Writer` -> `StreamConsumer|Producer`
- `StreamPropertiesReader|Writer` -> `StreamPropertiesConsumer|Producer`
- `StreamParametersReader|writer` -> `StreamTimeseriesConsumer|Producer`  (see section below about `Parameters`->`TimeSeries` rename)
- `StreamEventsReader|Writer` -> `StreamEventsConsumer|Producer`
- `ParametersBufferReader|Writer` -> `TimeseriesConsumer|Producer` (see section below about `Parameters`->`TimeSeries` rename)

### Additional renamings
- ParameterData renamed to TimeseriesData
- Write methods renamed to Publish
- StartReading renamed to Subscribe

### Event changes

Certain callbacks have altered signatures, either in the name or number of arguments. Detecting these changes will be straightforward, and thus, the specifics will be omitted.