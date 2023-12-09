using System;
using System.Collections.Generic;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Streaming.Models.StreamConsumer
{
    /// <summary>
    /// Interface for consumer of event streams, which raises <see cref="EventData"/> and <see cref="EventDefinitions"/> related messages
    /// </summary>
    public interface IStreamEventsConsumer : IDisposable
    {
        /// <summary>
        /// Raised when an events data package is received for the stream
        /// </summary>
        event EventHandler<EventDataReadEventArgs> OnDataReceived;

        /// <summary>
        /// Raised when the event definitions have changed for the stream.
        /// See <see cref="StreamEventsConsumer.Definitions"/> for the latest set of event definitions
        /// </summary>
        event EventHandler<EventDefinitionsChangedEventArgs> OnDefinitionsChanged;

        /// <summary>
        /// Gets the latest set of event definitions
        /// </summary>
        IList<EventDefinition> Definitions { get; }
    }
}