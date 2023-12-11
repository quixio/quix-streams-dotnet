using System;
using System.Collections.Generic;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Streaming.Models.StreamProducer
{
    /// <summary>
    /// Helper class for producing <see cref="EventDefinitions"/> and <see cref="EventData"/>
    /// </summary>
    public interface IStreamEventsProducer : IDisposable
    {
        /// <summary>
        /// Default epoch used for Timestamp event values. Datetime added on top of all the Timestamps.
        /// </summary>
        DateTime Epoch { get; set; }

        /// <summary>
        /// Default Tags injected to all Event Values sent by the producer.
        /// </summary>
        Dictionary<string, string> DefaultTags { get; set; }

        /// <summary>
        /// Default Location of the events. Event definitions added with <see cref="AddDefinition"/> will be inserted at this location.
        /// See <see cref="AddLocation"/> for adding definitions at a different location without changing default.
        /// Example: "/Group1/SubGroup2"
        /// </summary>
        string DefaultLocation { get; set; }

        /// <summary>
        /// Starts adding a new set of event values at the given timestamp.
        /// Note, <see cref="StreamEventsProducer.Epoch"/> is not used when invoking with <see cref="DateTime"/>
        /// </summary>
        /// <param name="dateTime">The datetime to use for adding new event values</param>
        /// <returns>Event data builder to add event values at the provided time</returns>
        EventDataBuilder AddTimestamp(DateTime dateTime);

        /// <summary>
        /// Starts adding a new set of event values at the given timestamp.
        /// </summary>
        /// <param name="timeSpan">The time since the default <see cref="StreamEventsProducer.Epoch"/> to add the event values at</param>
        /// <returns>Event data builder to add event values at the provided time</returns>
        EventDataBuilder AddTimestamp(TimeSpan timeSpan);

        /// <summary>
        /// Starts adding a new set of event values at the given timestamp.
        /// </summary>
        /// <param name="timeMilliseconds">The time in milliseconds since the default <see cref="StreamEventsProducer.Epoch"/> to add the event values at</param>
        /// <returns>Event data builder to add event values at the provided time</returns>
        EventDataBuilder AddTimestampMilliseconds(long timeMilliseconds);

        /// <summary>
        /// Starts adding a new set of event values at the given timestamp.
        /// </summary>
        /// <param name="timeNanoseconds">The time in nanoseconds since the default <see cref="StreamEventsProducer.Epoch"/> to add the event values at</param>
        /// <returns>Event data builder to add event values at the provided time</returns>
        EventDataBuilder AddTimestampNanoseconds(long timeNanoseconds);

        /// <summary>
        /// Adds a list of definitions to the <see cref="StreamEventsProducer"/>. Configure it with the builder methods.
        /// </summary>
        /// <param name="definitions">List of definitions</param>
        void AddDefinitions(List<EventDefinition> definitions);

        /// <summary>
        /// Add new Event definition to define properties like Name or Level, among others.
        /// </summary>
        /// <param name="eventId">Event Id. This must match the event id you use to Event values</param>
        /// <param name="name">Human friendly display name of the event</param>
        /// <param name="description">Description of the event</param>
        /// <returns>Event definition builder to define the event properties</returns>
        EventDefinitionBuilder AddDefinition(string eventId, string name = null, string description = null);

        /// <summary>
        /// Adds a new Location in the event groups hierarchy.
        /// </summary>
        /// <param name="location">The group location</param>
        EventDefinitionBuilder AddLocation(string location);

        /// <summary>
        /// Immediately writes the event definitions from the buffer without waiting for buffer condition to fulfill (200ms timeout)
        /// </summary>
        void Flush();

        /// <summary>
        /// Publish an event into the stream.
        /// </summary>
        /// <param name="data">Event to publish</param>
        void Publish(EventData data);

        /// <summary>
        /// Publish events into the stream.
        /// </summary>
        /// <param name="events">Events to publish</param>
        void Publish(ICollection<EventData> events);
    }
}