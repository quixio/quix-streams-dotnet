namespace QuixStreams.Streaming.Models.StreamProducer
{
    /// <summary>
    /// Provides extension methods for <see cref="IStreamProducer"/>.
    /// </summary>
    public static class StreamTimeseriesProducerExtensions
    {
        /// <summary>
        /// Creates a new <see cref="LeadingEdgeBuffer"/> using the <paramref name="producer"/> where tags form part of
        /// the row's key and can't be modified after initial values
        /// </summary>
        /// <param name="producer">The timeseries stream producer</param>
        /// <param name="leadingEdgeDelayMs">Leading edge delay configuration in Milliseconds</param>
        public static LeadingEdgeBuffer CreateLeadingEdgeBuffer(
            this IStreamTimeseriesProducer producer,
            int leadingEdgeDelayMs)
        {
            return new LeadingEdgeBuffer(producer, leadingEdgeDelayMs);
        }

        /// <summary>
        /// Creates a new <see cref="LeadingEdgeTimeBuffer"/> using the <paramref name="producer"/> where tags do not
        /// form part of the row's key and can be freely modified after initial values
        /// </summary>
        /// <param name="producer">The timeseries stream producer</param>
        /// <param name="leadingEdgeDelayMs">Leading edge delay configuration in Milliseconds</param>
        public static LeadingEdgeTimeBuffer CreateLeadingEdgeTimeBuffer(
            this IStreamTimeseriesProducer producer,
            int leadingEdgeDelayMs)
        {
            return new LeadingEdgeTimeBuffer(producer, leadingEdgeDelayMs);
        }
    }
}