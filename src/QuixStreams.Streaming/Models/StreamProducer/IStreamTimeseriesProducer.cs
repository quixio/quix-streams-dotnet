using System;
using System.Collections.Generic;

namespace QuixStreams.Streaming.Models.StreamProducer
{
    /// <summary>
    /// Helper class for producing <see cref="ParameterDefinition"/> and <see cref="TimeseriesData"/>
    /// </summary>
    public interface IStreamTimeseriesProducer : IDisposable
    {
        /// <summary>
        /// Gets the buffer for producing timeseries data
        /// </summary>
        TimeseriesBufferProducer Buffer { get; }

        /// <summary>
        /// Default Location of the parameters. Parameter definitions added with <see cref="AddDefinition"/> will be inserted at this location.
        /// See <see cref="AddLocation"/> for adding definitions at a different location without changing default.
        /// Example: "/Group1/SubGroup2"
        /// </summary>
        string DefaultLocation { get; set; }

        /// <summary>
        /// Publish data to stream without any buffering
        /// </summary>
        /// <param name="data">Timeseries data to publish</param>
        void Publish(TimeseriesData data);

        /// <summary>
        /// Publish data in TimeseriesDataRaw format without any buffering
        /// </summary>
        /// <param name="data">Timeseries data to publish</param>
        void Publish(QuixStreams.Telemetry.Models.TimeseriesDataRaw data);

        /// <summary>
        /// Publish single timestamp to stream without any buffering
        /// </summary>
        /// <param name="timestamp">Timeseries timestamp to publish</param>
        void Publish(TimeseriesDataTimestamp timestamp);

        /// <summary>
        /// Adds a list of definitions to the <see cref="StreamTimeseriesProducer"/>. Configure it with the builder methods.
        /// </summary>
        /// <param name="definitions">List of definitions</param>
        void AddDefinitions(List<ParameterDefinition> definitions);

        /// <summary>
        /// Adds a new parameter definition to the <see cref="StreamTimeseriesProducer"/>. Configure it with the builder methods.
        /// </summary>
        /// <param name="parameterId">The id of the parameter. Must match the parameter id used to send data.</param>
        /// <param name="name">The human friendly display name of the parameter</param>
        /// <param name="description">The description of the parameter</param>
        /// <returns>Parameter definition builder to define the parameter properties</returns>
        ParameterDefinitionBuilder AddDefinition(string parameterId, string name = null, string description = null);

        /// <summary>
        /// Adds a new location in the parameters groups hierarchy
        /// </summary>
        /// <param name="location">The group location</param>
        /// <returns>Parameter definition builder to define the parameters under the specified location</returns>
        ParameterDefinitionBuilder AddLocation(string location);

        /// <summary>
        /// Immediately publish timeseries data and definitions from the buffer without waiting for buffer condition to fulfill for either
        /// </summary>
        void Flush();

        /// <summary>
        /// Creates a new <see cref="LeadingEdgeBuffer"/> using this producer where tags form part of the row's key
        /// and can't be modified after initial values
        /// </summary>
        /// <param name="leadingEdgeDelayMs">Leading edge delay configuration in Milliseconds</param>
        LeadingEdgeBuffer CreateLeadingEdgeBuffer(int leadingEdgeDelayMs);

        /// <summary>
        /// Creates a new <see cref="LeadingEdgeTimeBuffer"/> using this producer where tags do not form part of the row's key
        /// and can be freely modified after initial values
        /// </summary>
        /// <param name="leadingEdgeDelayMs">Leading edge delay configuration in Milliseconds</param>
        LeadingEdgeTimeBuffer CreateLeadingEdgeTimeBuffer(int leadingEdgeDelayMs);
    }
}