![Quix - React to data, fast](https://github.com/quixio/quix-streams-dotnet/blob/main/images/quixstreams-banner.jpg)

[![Quix on Twitter](https://img.shields.io/twitter/url?label=Twitter&style=social&url=https%3A%2F%2Ftwitter.com%2Fquix_io)](https://twitter.com/quix_io)
[![The Stream Community Slack](https://img.shields.io/badge/-The%20Stream%20Slack-blueviolet)](https://quix.io/slack-invite)
[![Linkedin](https://img.shields.io/badge/LinkedIn-0A66C2.svg?logo=linkedin)](https://www.linkedin.com/company/70925173/)
[![Events](https://img.shields.io/badge/-Events-blueviolet)](https://quix.io/community#events)
[![YouTube](https://img.shields.io/badge/YouTube-FF0000.svg?logo=youtube)](https://www.youtube.com/channel/UCrijXvbQg67m9-le28c7rPA)

# Quix Streams for .NET

Quix Streams for .NET is a Kafka streaming client library used mainly for Quix internal platform needs and existing .NET integrations.

This repository is in maintenance mode. It remains published because it is still useful for .NET services that need to produce or consume Quix Streams-compatible data, but Quix's primary open source focus is now the Python library:

**Use [quixio/quix-streams](https://github.com/quixio/quix-streams) for new Quix Streams projects.**

The Python library is where active feature development happens first. No major new work is expected in this .NET repository, but we aim to keep it updated for internal platform needs and community contributions.

## When to Use This Library

Use this package if you:

- Have an existing .NET application using Quix Streams.
- Need a C# producer or consumer for Quix Streams-compatible Kafka data.
- Are integrating with Quix platform services that already depend on this library.
- Need lower-level Kafka transport building blocks. The library includes practical handling for Kafka edge cases found in production use, and other libraries can be built on top of `QuixStreams.Kafka.Transport`.
- Want the convenience APIs in `QuixStreams.Streaming` or telemetry helpers for working with Kafka messages in .NET.

For complex stream processing or new stream processing applications, start with the Python library instead: [github.com/quixio/quix-streams](https://github.com/quixio/quix-streams).

## Install

Install the .NET package from NuGet:

[QuixStreams.Streaming](https://www.nuget.org/packages/QuixStreams.Streaming)

## Documentation

The documentation in this repository is retained for existing .NET users:

- [Quickstart](docs/quickstart.md)
- [Connect to Kafka or Quix Cloud](docs/connect.md)
- [Publish data](docs/publish.md)
- [Subscribe to data](docs/subscribe.md)
- [Kafka and Quix Streams](docs/kafka.md)

Some docs may describe the broader Quix Streams model rather than the current direction of this repository. For current Quix Streams development, use the Python project: [quixio/quix-streams](https://github.com/quixio/quix-streams).

## Compatibility

This library is intended to remain compatible with Quix Streams data produced and consumed by Quix platform services and the primary Python library where applicable. Compatibility work is driven by platform needs, maintenance, and community contributions rather than a separate .NET feature roadmap.

## Using Quix Streams with Quix Cloud

This library does not require a commercial product. When used with the [Quix platform](https://www.quix.io), it can integrate with Quix-managed Kafka, configuration, monitoring, data exploration, pipeline visualization, and related platform workflows.

## Contributing

Community feedback and fixes are welcome, especially for bugs, compatibility issues, and documentation improvements. Before opening a larger PR, please create an issue so we can discuss whether the change fits the maintenance scope of this repository.

Read the [Contributing Guide](CONTRIBUTING.md) for local development and PR guidance.

## Need Help?

If you run into a problem, ask in [The Stream Slack community](https://quix.io/slack-invite) or create an [issue](https://github.com/quixio/quix-streams-dotnet/issues) in this repository.

For help with new Quix Streams projects, please start with the Python library: [github.com/quixio/quix-streams](https://github.com/quixio/quix-streams).

## Community

Join other software engineers in [The Stream Slack community](https://quix.io/slack-invite), an online community for people interested in data streaming.

You can also follow Quix on [Twitter](https://twitter.com/quix_io), [LinkedIn](https://www.linkedin.com/company/70925173), and [YouTube](https://www.youtube.com/channel/UCrijXvbQg67m9-le28c7rPA).

## License

Quix Streams for .NET is licensed under the Apache 2.0 license. See [LICENSE](LICENSE).
