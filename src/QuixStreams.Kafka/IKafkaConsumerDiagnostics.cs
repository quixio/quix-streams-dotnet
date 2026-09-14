using System;

namespace QuixStreams.Kafka
{
    public enum ConsumerLifecycleState { Stopped, Starting, Running, Recovering, Failed }

    /// <summary>Optional diagnostics. Notifications run serially on the thread pool, outside the
    /// lifecycle owner. Handlers may call Close; exceptions are logged and isolated. Slow handlers
    /// delay later notifications. Read Snapshot for current state.</summary>
    public interface IKafkaConsumerDiagnostics
    {
        ConsumerDiagnosticsSnapshot Snapshot { get; }
        event EventHandler<ConsumerDiagnosticsSnapshot>? DiagnosticsChanged;
    }

    public sealed class ConsumerDiagnosticsSnapshot : EventArgs
    {
        public string ConsumerId { get; }
        public ConsumerLifecycleState State { get; }
        public long Generation { get; }
        public Guid? RecoveryEpisodeId { get; }
        public int AttemptCount { get; }
        public DateTimeOffset StateChangedAt { get; }
        public string? LastFailureCode { get; }
        public string? LastFailureReason { get; }
        public bool AssignmentSettled { get; }
        public int AssignmentCount { get; }
        public DateTimeOffset? LastSuccessfulPollAt { get; }
        public DateTimeOffset? LastRecordAt { get; }

        internal ConsumerDiagnosticsSnapshot(string id, ConsumerLifecycleState state, long generation,
            Guid? episode, int attempts, DateTimeOffset changed, string? code, string? reason,
            bool settled, int count, DateTimeOffset? poll, DateTimeOffset? record)
        {
            ConsumerId = id; State = state; Generation = generation; RecoveryEpisodeId = episode;
            AttemptCount = attempts; StateChangedAt = changed; LastFailureCode = code; LastFailureReason = reason;
            AssignmentSettled = settled; AssignmentCount = count; LastSuccessfulPollAt = poll; LastRecordAt = record;
        }
    }
}
