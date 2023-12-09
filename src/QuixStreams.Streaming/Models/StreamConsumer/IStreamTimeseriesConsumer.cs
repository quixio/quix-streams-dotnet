using System;
using System.Collections.Generic;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Streaming.Models.StreamConsumer
{
    /// <summary>
    /// Consumer for streams, which raises <see cref="TimeseriesData"/> and <see cref="ParameterDefinitions"/> related messages
    /// </summary>
    public interface IStreamTimeseriesConsumer : IDisposable
    {
        /// <summary>
        /// Creates a new buffer for reading data
        /// </summary>
        /// <param name="bufferConfiguration">An optional TimeseriesBufferConfiguration</param>
        /// <param name="parametersFilter">Zero or more parameter identifiers to filter as a whitelist. If provided, only those parameters will be available through this buffer</param>
        /// <returns><see cref="TimeseriesBufferConsumer"/> which will raise OnDataReceived event when new data is consumed</returns>
        TimeseriesBufferConsumer CreateBuffer(TimeseriesBufferConfiguration bufferConfiguration = null, params string[] parametersFilter);

        /// <summary>
        /// Creates a new buffer for reading data
        /// </summary>
        /// <param name="parametersFilter">Zero or more parameter identifiers to filter as a whitelist. If provided, only those parameters will be available through this buffer</param>
        /// <returns><see cref="TimeseriesBufferConsumer"/> which will raise OnDataReceived event when new data is consumed</returns>
        TimeseriesBufferConsumer CreateBuffer(params string[] parametersFilter);

        /// <summary>
        /// Raised when the parameter definitions have changed for the stream.
        /// See <see cref="StreamTimeseriesConsumer.Definitions"/> for the latest set of parameter definitions
        /// </summary>
        event EventHandler<ParameterDefinitionsChangedEventArgs> OnDefinitionsChanged;

        /// <summary>
        /// Event raised when data is received (without buffering)
        /// This event does not use Buffers, and data will be raised as they arrive without any processing.
        /// </summary>
        event EventHandler<TimeseriesDataReadEventArgs> OnDataReceived;

        /// <summary>
        /// Event raised when data is received (without buffering) in raw transport format
        /// This event does not use Buffers, and data will be raised as they arrive without any processing.
        /// </summary>
        event EventHandler<TimeseriesDataRawReadEventArgs> OnRawReceived;

        /// <summary>
        /// Gets the latest set of parameter definitions
        /// </summary>
        List<ParameterDefinition> Definitions { get; }
    }
}