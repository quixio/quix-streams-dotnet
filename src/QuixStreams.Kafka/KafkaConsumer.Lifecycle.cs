using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace QuixStreams.Kafka
{
    public partial class KafkaConsumer
    {
        private readonly object lifecycleLock = new object();
        private readonly object diagnosticsLock = new object();
        private readonly AsyncLocal<bool> ownerContext = new AsyncLocal<bool>();
        private readonly ConsumerRecoveryPolicy recoveryPolicy;
        internal Func<ConsumerBuilder<byte[]?, byte[]>, IConsumer<byte[]?, byte[]>> ConsumerFactory = b => b.Build();
        internal Func<DateTimeOffset> UtcNow = () => DateTimeOffset.UtcNow;
        internal Func<TimeSpan, CancellationToken, Task> Delay = Task.Delay;
        private ConsumerDiagnosticsSnapshot? snapshot;
        private Task notifications = Task.CompletedTask;
        private DateTimeOffset? revokeDeadline;
        private Timer? episodeTimer;
        private long generation;
        private Guid? episode;
        private DateTimeOffset? episodeStarted;
        private int attempts;
        private DateTimeOffset initializationStarted;
        private bool assignmentSettled;
        private HashSet<TopicPartition> assignment = new HashSet<TopicPartition>();
        private HashSet<TopicPartition> initialized = new HashSet<TopicPartition>();
        private DateTimeOffset? lastPoll, lastRecord;
        private string? failureCode, failureReason;
        public event EventHandler<ConsumerDiagnosticsSnapshot>? DiagnosticsChanged;
        public ConsumerDiagnosticsSnapshot Snapshot
        {
            get { lock (diagnosticsLock) return snapshot ?? (snapshot = MakeSnapshot(ConsumerLifecycleState.Stopped, UtcNow())); }
        }
        private ConsumerDiagnosticsSnapshot MakeSnapshot(ConsumerLifecycleState state, DateTimeOffset changed) =>
            new ConsumerDiagnosticsSnapshot(configId, state, generation, episode, attempts, changed,
                failureCode, failureReason, assignmentSettled, assignment.Count, lastPoll, lastRecord);
        private void RefreshSnapshot() { lock (diagnosticsLock) snapshot = MakeSnapshot(Snapshot.State, Snapshot.StateChangedAt); }
        private void Publish(ConsumerLifecycleState state)
        {
            lock (diagnosticsLock)
            {
                if (Snapshot.State == ConsumerLifecycleState.Failed && state != ConsumerLifecycleState.Stopped) return;
                var changed = Snapshot.State == state ? Snapshot.StateChangedAt : UtcNow();
                snapshot = MakeSnapshot(state, changed);
                var value = snapshot;
                logger.LogInformation("Consumer {ConsumerId} generation {Generation}: {State}, attempt {Attempt}, code {Code}", configId, generation, state, attempts, failureCode);
                var handlers = DiagnosticsChanged;
                using (ExecutionContext.SuppressFlow())
                    notifications = notifications.ContinueWith(_ =>
                    {
                        if (handlers == null) return;
                        foreach (EventHandler<ConsumerDiagnosticsSnapshot> handler in handlers.GetInvocationList())
                            try { handler(this, value); } catch (Exception ex) { logger.LogError(ex, "Consumer diagnostic subscriber failed"); }
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
        }
        private void NotifyError(Exception ex)
        {
            var handlers = OnErrorOccurred;
            using (ExecutionContext.SuppressFlow())
                Task.Run(() =>
                {
                    if (handlers == null) return;
                    foreach (EventHandler<Exception> handler in handlers.GetInvocationList())
                        try { handler(this, ex); } catch (Exception subscriber) { logger.LogError(subscriber, "Consumer error subscriber failed"); }
                });
        }
        // Confluent exposes no structured operation field. Match only the known OffsetFetch
        // diagnostic in addition to the protocol code; arbitrary coordinator errors are excluded.
        internal static bool IsOffsetInitializationFailure(Error error) => error.Code == ErrorCode.NotCoordinatorForGroup &&
            (error.Reason.IndexOf("Failed to fetch committed offsets", StringComparison.OrdinalIgnoreCase) >= 0 ||
             error.Reason.IndexOf("OffsetFetch", StringComparison.OrdinalIgnoreCase) >= 0);
        private void Fail(string code, string reason)
        { lock (diagnosticsLock) { failureCode = code; failureReason = reason; Publish(ConsumerLifecycleState.Failed); } }
        private void RequestRecovery(string code, string reason)
        {
            lock (diagnosticsLock)
            {
                if (closing || disposed || Snapshot.State == ConsumerLifecycleState.Failed) return;
                failureCode = code; failureReason = reason;
                if (!episode.HasValue)
                {
                    episode = Guid.NewGuid(); episodeStarted = UtcNow(); attempts = 0;
                    var id = episode;
                    episodeTimer = new Timer(_ =>
                    {
                        lock (diagnosticsLock)
                            if (episode == id && !closing)
                            { Fail("RecoveryTimeout", "Recovery episode budget exhausted"); disconnected = true; }
                    }, null, recoveryPolicy.EpisodeTimeout, Timeout.InfiniteTimeSpan);
                }
                if (!disconnected) Publish(ConsumerLifecycleState.Recovering);
                disconnected = true;
            }
        }
        private void SetAssignment(List<TopicPartition>? partitions)
        {
            lock (diagnosticsLock)
            {
                assignmentSettled = partitions != null;
                assignment = new HashSet<TopicPartition>(partitions ?? new List<TopicPartition>());
                initialized.Clear();
                initializationStarted = UtcNow();
                if (!closing) Publish(episode.HasValue ? ConsumerLifecycleState.Recovering : ConsumerLifecycleState.Starting);
            }
        }
        private void ConfirmReady()
        {
            lock (diagnosticsLock)
            {
                if (disconnected || closing || !assignmentSettled || !assignment.IsSubsetOf(initialized)) return;
                if (Snapshot.State == ConsumerLifecycleState.Running) return;
                episodeTimer?.Dispose(); episodeTimer = null;
                episode = null; episodeStarted = null; attempts = 0;
                Publish(ConsumerLifecycleState.Running);
                connectionEstablishedEvent.Set();
            }
        }
        public void Open()
        {
            lock (lifecycleLock)
            {
                if (disposed) throw new ObjectDisposedException(nameof(KafkaConsumer));
                if (workerTask != null && !workerTask.IsCompleted) return;
                lock (diagnosticsLock)
                {
                    closing = false; disconnected = false; canReconnect = true;
                    episodeTimer?.Dispose(); episodeTimer = null;
                    episode = null; episodeStarted = null; attempts = 0;
                    failureCode = null; failureReason = null;
                }
                workerTaskCts?.Dispose();
                workerTaskCts = new CancellationTokenSource();
                var ct = workerTaskCts.Token;
                connectionEstablishedEvent.Reset();
                Publish(ConsumerLifecycleState.Stopped);
                Publish(ConsumerLifecycleState.Starting);
                workerTask = Task.Run(() => LifecycleWork(ct));
            }
            if (VerifyBrokerConnection && !ownerContext.Value) connectionEstablishedEvent.Wait(TimeSpan.FromSeconds(5));
        }
        private async Task LifecycleWork(CancellationToken ct)
        {
            ownerContext.Value = true;
            try
            {
                while (!ct.IsCancellationRequested && Snapshot.State != ConsumerLifecycleState.Failed)
                {
                    if (episode.HasValue)
                    {
                        if (!canReconnect || attempts >= recoveryPolicy.MaximumAttempts || UtcNow() - episodeStarted!.Value >= recoveryPolicy.EpisodeTimeout)
                        { Fail("RecoveryExhausted", "Recovery attempts or episode budget exhausted"); break; }
                        if (attempts > 0)
                        {
                            var delay = TimeSpan.FromMilliseconds(Math.Min(recoveryPolicy.MaximumBackoff.TotalMilliseconds,
                                recoveryPolicy.InitialBackoff.TotalMilliseconds * Math.Pow(2, attempts - 1)));
                            var remaining = recoveryPolicy.EpisodeTimeout - (UtcNow() - episodeStarted.Value);
                            await Delay(delay < remaining ? delay : remaining, ct);
                            if (UtcNow() - episodeStarted.Value >= recoveryPolicy.EpisodeTimeout) { Fail("RecoveryTimeout", "Recovery episode budget exhausted"); break; }
                        }
                        attempts++;
                    }
                    ct.ThrowIfCancellationRequested();
                    if (Snapshot.State == ConsumerLifecycleState.Failed) break;
                    lock (diagnosticsLock)
                    {
                        if (Snapshot.State == ConsumerLifecycleState.Failed) break;
                        disconnected = false;
                        generation++;
                        assignmentSettled = false; assignment.Clear(); initialized.Clear(); lastPoll = null; lastRecord = null;
                        initializationStarted = UtcNow();
                        Publish(episode.HasValue ? ConsumerLifecycleState.Recovering : ConsumerLifecycleState.Starting);
                    }
                    workerTaskPollFinished = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    try
                    {
                        CreateConsumer();
                        if (!ct.IsCancellationRequested && !closing && consumer != null)
                            await PollingWork(consumer, ct);
                    }
                    catch (KafkaException ex)
                    {
                        if (ex.Error.IsFatal) Fail(ex.Error.Code.ToString(), "Fatal consumer creation error");
                        else RequestRecovery(ex.Error.Code.ToString(), "Consumer creation failed");
                        NotifyError(ex);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                    catch (Exception ex) { Fail("UnexpectedPollFailure", "Unexpected lifecycle failure"); NotifyError(ex); }
                    finally
                    {
                        workerTaskPollFinished.TrySetResult(null);
                        // Polling has stopped. Revocation clears transport fragments and commit tracking
                        // before a replacement can deliver replayed records.
                        var old = consumer;
                        if (old != null)
                        {
                            try
                            {
                                // Preserve graceful shutdown's final processed-offset commit hook.
                                // Recovery deliberately permits replay of uncommitted packages.
                                if (closing)
                                    OnRevoking?.Invoke(this, new RevokingEventArgs(old.Assignment.Select(p =>
                                        new TopicPartitionOffset(p, old.Position(p))).ToList()));
                            }
                            finally
                            {
                                // Wait for any existing public Commit, then reject further commits.
                                // Never hold this lock across transport callbacks (AutoCommitter has
                                // its own commit lock) or a native close.
                                lock (consumerLock) consumer = null;
                                try
                                {
                                    lastRevokeCancelAction?.Invoke();
                                    lastRevokingState = null; seekFunc = _ => false;
                                    OnRevoked?.Invoke(this, new RevokedEventArgs(old.Assignment.Union(assignment).Select(p => new TopicPartitionOffset(p, Offset.Unset)).ToList()));
                                }
                                finally
                                {
                                    try { old.Close(); }
                                    finally { old.Dispose(); }
                                }
                            }
                        }
                    }
                    if (Snapshot.State == ConsumerLifecycleState.Failed || ct.IsCancellationRequested) break;
                    if (!episode.HasValue) RequestRecovery("Disconnected", "Consumer disconnected");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) { Fail("LifecycleFailure", "Consumer cleanup failed"); NotifyError(ex); }
            finally
            {
                workerTaskPollFinished?.TrySetResult(null);
                if (ct.IsCancellationRequested && Snapshot.State != ConsumerLifecycleState.Failed) Publish(ConsumerLifecycleState.Stopped);
                episodeTimer?.Dispose();
                ownerContext.Value = false;
            }
        }
    }
}
