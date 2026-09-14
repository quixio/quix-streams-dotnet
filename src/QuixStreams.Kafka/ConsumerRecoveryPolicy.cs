using System;

namespace QuixStreams.Kafka
{
    public sealed class ConsumerRecoveryPolicy
    {
        public int MaximumAttempts { get; set; } = 5;
        public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan MaximumBackoff { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan EpisodeTimeout { get; set; } = TimeSpan.FromSeconds(180);
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

        internal ConsumerRecoveryPolicy ValidateAndCopy()
        {
            if (MaximumAttempts <= 0 || InitialBackoff <= TimeSpan.Zero || MaximumBackoff < InitialBackoff ||
                EpisodeTimeout <= TimeSpan.Zero || InitializationTimeout <= TimeSpan.Zero || ShutdownTimeout <= TimeSpan.Zero ||
                ShutdownTimeout.TotalMilliseconds > int.MaxValue || EpisodeTimeout.TotalMilliseconds > int.MaxValue ||
                MaximumBackoff.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(ConsumerRecoveryPolicy));
            return (ConsumerRecoveryPolicy)MemberwiseClone();
        }
    }
}
