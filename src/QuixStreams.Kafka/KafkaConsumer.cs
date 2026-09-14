using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace QuixStreams.Kafka
{
    /// <summary>
    /// Kafka consumer implemented using polling mechanism from Kafka
    /// </summary>
    public partial class KafkaConsumer : IKafkaConsumer, IKafkaConsumerDiagnostics
    {
        private readonly ILogger logger = Logging.CreateLogger<KafkaConsumer>();
        private readonly ConsumerConfig config;

        private readonly object consumerLock = new object();

        private readonly ConsumerTopicConfiguration consumerTopicConfiguration;

        private IConsumer<byte[]?, byte[]>? consumer;
        private volatile bool disposed;
        private volatile bool closing;
        private volatile bool disconnected; // connection is deemed dead
        private bool canReconnect = true;

        private Task? workerTask;
        private TaskCompletionSource<object?>? workerTaskPollFinished; // Resolved when WorkerTask's polling loop is completed. Used for stopping more efficiently
        private CancellationTokenSource? workerTaskCts;
        private readonly Stopwatch currentPackageProcessTime = new Stopwatch();
        private readonly bool consumerGroupSet;
        private List<TopicPartitionOffset>? lastRevokingState;
        private Action? lastRevokeCompleteAction = null;
        private Action? lastRevokeCancelAction = null;
        private ShouldSkipConsumeResult seekFunc = (cr) => false; // return true if consume result should be dropped
        private const int RevokeTimeoutPeriodInMs = 5000; // higher number should only mean up to this seconds of delay if you are never going to get new assignments,
                                                          // but should not prove to cause any other issue
        
        private readonly ManualResetEventSlim connectionEstablishedEvent = new ManualResetEventSlim(false);

        
        private readonly bool checkForKeepAlivePackets;   // Enables the check for keep alive messages from Quix
        private readonly string configId; // Hash to use in logs, so it is easier to detect what is experiencing an issue

        /// <summary>
        /// Enables the custom re-assigned logic for when partition is revoked and then reassigned.
        /// If disabled, reverts to original kafka logic of always raising revoked partitions.
        /// Defaults to true.
        /// </summary>
        public bool EnableReAssignedLogic { get; set; } = true;

        /// <inheritdoc />
        public event EventHandler<Exception>? OnErrorOccurred;

        /// <inheritdoc/>
        public Func<KafkaMessage, Task> OnMessageReceived { get; set; }

        /// <inheritdoc/>
        public event EventHandler<CommittedEventArgs>? OnCommitted;
        
        
        /// <inheritdoc/>
        public event EventHandler<CommittingEventArgs>? OnCommitting;
        
        /// <summary>
        /// Raised when consumer is losing access to subscribed source depending on implementation
        /// Argument is the state which describes what is being revoked. State is of type <see cref="List{TopicPartitionOffset}"/>
        /// </summary>
        public event EventHandler<RevokingEventArgs>? OnRevoking;
        
        /// <summary>
        /// Raised when consumer lost access to subscribed source depending on implementation
        /// Argument is the state which describes what got revoked. State is of type <see cref="List{TopicPartitionOffset}"/>
        /// </summary>
        public event EventHandler<RevokedEventArgs>? OnRevoked;

        /// <summary>
        /// If set to false it won't wait for the broker verification
        /// </summary>
        public bool VerifyBrokerConnection { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of <see cref="KafkaConsumer"/>
        /// </summary>
        /// <param name="consumerConfiguration">The subscriber configuration to use</param>
        /// <param name="consumerTopicConfiguration">The topic configuration to use</param>
        public KafkaConsumer(ConsumerConfiguration consumerConfiguration,
            ConsumerTopicConfiguration consumerTopicConfiguration)
        {
            this.recoveryPolicy = consumerConfiguration.Recovery.ValidateAndCopy();
            this.consumerTopicConfiguration = consumerTopicConfiguration;
            this.config = GetKafkaConsumerConfig(consumerConfiguration);
            this.config.EnablePartitionEof = true;
            this.consumerGroupSet = consumerConfiguration.ConsumerGroupSet;
            this.checkForKeepAlivePackets = consumerConfiguration.CheckForKeepAlivePackets;
            this.configId = Guid.NewGuid().ToString("N");

            this.OnMessageReceived = message => Task.CompletedTask; 
            
        }

        private ConsumerConfig GetKafkaConsumerConfig(ConsumerConfiguration consumerConfiguration) => consumerConfiguration.ToConsumerConfig();

        private void CreateConsumer()
        {
                if (closing || disposed || Snapshot.State == ConsumerLifecycleState.Failed) return;
                var consumerBuilder = new ConsumerBuilder<byte[]?, byte[]>(this.config);
                consumerBuilder.SetErrorHandler(this.ConsumerErrorHandler);
                consumerBuilder.SetOffsetsCommittedHandler(this.AutomaticOffsetsCommittedHandler);
                consumerBuilder.SetStatisticsHandler(this.ConsumerStatisticsHandler);
                consumerBuilder.SetPartitionsAssignedHandler(this.PartitionsAssignedHandler);
                consumerBuilder.SetPartitionsRevokedHandler(this.PartitionsRevokedHandler);
                consumerBuilder.SetPartitionsLostHandler(this.PartitionsLostHandler);
                consumerBuilder.SetLogHandler(this.ConsumerLogHandler);

                var partitions = this.consumerTopicConfiguration.Partitions?.ToList();
                if (!this.consumerGroupSet)
                {
                    if (partitions == null)
                    {
                        Offset selectedOffset;
                        switch (this.config.AutoOffsetReset)
                        {
                            case AutoOffsetReset.Latest:
                                selectedOffset = Offset.End;
                                break;
                            case AutoOffsetReset.Earliest:
                                selectedOffset = Offset.Beginning;
                                break;
                            case AutoOffsetReset.Error:
                                selectedOffset = Offset.Unset; // doing unset instead of end, so we get nice logs below
                                break;
                            case null:
                                selectedOffset = Offset.Unset; // doing unset instead of end, so we get nice logs below
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        partitions = this.consumerTopicConfiguration.Topics?.Select(x => new TopicPartitionOffset(x, Partition.Any, selectedOffset)).ToList(); // doing unset instead of end, so we get nice logs below
                    }

                    if (partitions == null) throw new InvalidOperationException("The configuration for kafka consumer is not valid. Must define either topic or partition at least.");

                    // convert all "Partition Any" to explicit list of partition because Any doesn't work. // See https://github.com/confluentinc/confluent-kafka-dotnet/issues/917
                    if (partitions.Any(x => x.Partition == Partition.Any))
                    {
                        var newPartitions = new List<TopicPartitionOffset>();
                        IAdminClient? adminClient = null;
                        try
                        {
                            foreach (var partition in partitions)
                            {
                                if (partition.Partition != Partition.Any)
                                {
                                    newPartitions.Add(partition);
                                    continue;
                                }
                                this.logger.LogTrace("[{0}] Topic '{1}' with partition {2} requires metadata retrieval", this.configId, partition.Topic, partition.Partition);
                                if (adminClient == null)
                                {
                                    this.logger.LogTrace("[{0}] Creating admin client to retrieve metadata", this.configId);
                                    void NullLoggerForAdminLogs(IAdminClient adminClient, LogMessage logMessage)
                                    {
                                        // Log nothing
                                    }
                                    adminClient = GetAdminClientBuilder(this.config).SetLogHandler(NullLoggerForAdminLogs).Build();
                                }
                                var metadata = adminClient.GetMetadata(partition.Topic, TimeSpan.FromSeconds(5));
                                if (metadata == null)
                                {
                                    throw new OperationCanceledException($"[{this.configId}] Topic '{partition.Topic}' with partition {partition.Partition} requires metadata retrieval, which timed out. Try again or specify partitions explicitly."); // Maybe a more specific exception ?
                                }
                                this.logger.LogTrace("[{0}] Retrieved metadata for Topic {1} with partition {2}", this.configId, partition.Topic, partition.Partition);
                                var topicMetaData = metadata.Topics.FirstOrDefault(x => x.Topic == partition.Topic);
                                if (topicMetaData == null)
                                {
                                    throw new Exception($"[{this.configId}] Failed to retrieve metadata for topic '{partition.Topic}' with partition {partition.Partition}. Try again or specify partitions explicitly."); // Maybe a more specific exception ?
                                }
                                
                                if (topicMetaData.Partitions.Count == 0)
                                {
                                    throw new OperationCanceledException($"[{this.configId}] Found no partition information for topic '{partition.Topic}'. Verify the topic exists and try again or use consumer group."); // Maybe a more specific exception ?
                                }

                                newPartitions.AddRange(topicMetaData.Partitions.Select(x=> new TopicPartitionOffset(new TopicPartition(partition.Topic, x.PartitionId), partition.Offset)));
                            }
                        }
                        finally
                        {
                            if (adminClient != null)
                            {
                                adminClient.Dispose();
                                this.logger.LogTrace("[{0}] Disposed admin client used to retrieve metadata", this.configId);
                            }
                        }

                        partitions = newPartitions;
                    }

                    // only allow OFFSET_BEGINNING, OFFSET_END or absolute offset, see https://github.com/confluentinc/confluent-kafka-python/issues/250
                    for (var index = 0; index < partitions.Count; index++)
                    {
                        var partition = partitions[index];
                        if (partition.Offset == Offset.Unset)
                        {
                            this.logger.LogDebug("[{0}] Topic '{1}' with partition {2} defaults to end, because neither offset or group was specified.", this.configId, partition.Topic, partition.Partition);
                            partition = new TopicPartitionOffset(partition.Topic, partition.Partition, Offset.End);
                            partitions[index] = partition;
                            continue;
                        }
                        if (partition.Offset == Offset.Stored)
                        {
                            this.logger.LogWarning("[{0}] Topic '{1}' with partition {2} is unable to use last stored offset, because group was not specified. Defaulting to end.", this.configId, partition.Topic, partition.Partition);
                            partition = new TopicPartitionOffset(partition.Topic, partition.Partition, Offset.End);
                            partitions[index] = partition;
                        }
                    }
                }
                
                this.consumer = ConsumerFactory(consumerBuilder);
                if (closing || disposed || Snapshot.State == ConsumerLifecycleState.Failed) return;
                
                if (partitions != null)
                {
                    this.logger.LogTrace("[{0}] Assigning partitions {1} to consumer", this.configId, string.Join(",",partitions.Select(y=> y.TopicPartition.ToString())));
                    this.consumer.Assign(partitions);
                    SetAssignment(partitions.Select(p => p.TopicPartition).ToList());
                    this.logger.LogTrace("[{0}] Assigned partitions {1} to consumer", this.configId, string.Join(",",partitions.Select(y=> y.TopicPartition.ToString())));
                }
                else
                {
                    this.logger.LogTrace("[{0}] Assigning topics to consumer", this.configId);
                    this.consumer.Subscribe(this.consumerTopicConfiguration.Topics);
                    this.logger.LogTrace("[{0}] Assigned topics to consumer", this.configId);
                }

        }

        private AdminClientBuilder GetAdminClientBuilder(ConsumerConfig config)
        {
            var filteredConfig = config.Where(prop =>
                    !prop.Key.StartsWith("dotnet.producer.") &&
                    !prop.Key.StartsWith("dotnet.consumer."))
                .ToDictionary(k => k.Key, v => v.Value);
            
            var adminConfig = new ConsumerConfig(filteredConfig);

            return new AdminClientBuilder(adminConfig);
        }

        private void AutomaticOffsetsCommittedHandler(IConsumer<byte[]?, byte[]> consumer, CommittedOffsets offsets)
        {
            if (this.logger.IsEnabled(LogLevel.Trace))
            {
                foreach (var committedOffset in offsets.Offsets)
                {
                    logger.LogTrace("[{0}] Auto committed offset {1} for Topic {2}, Partition {3}, with result {4}", this.configId, committedOffset.Offset, committedOffset.Topic, committedOffset.Partition, committedOffset.Error);   
                }
            }
        }

        private void ConsumerLogHandler(IConsumer<byte[]?, byte[]> consumer, LogMessage msg)
        {
            switch (msg.Level)
            {
                case SyslogLevel.Alert:
                case SyslogLevel.Warning:
                    logger.LogWarning("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
                case SyslogLevel.Emergency:
                case SyslogLevel.Critical:
                    logger.LogCritical("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
                case SyslogLevel.Error:
                    logger.LogError("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogInformation("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
                case SyslogLevel.Debug:
                    logger.LogDebug("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
                default:
                    logger.LogDebug("[{0}][Kafka log][{1}] {2}", this.configId, msg.Facility, msg.Message);
                    break;
            } 
        }
        
        private void PartitionsLostHandler(IConsumer<byte[]?, byte[]> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            if (!ReferenceEquals(consumer, this.consumer)) return;
            SetAssignment(null);
            try
            {
                this.lastRevokeCancelAction?.Invoke();
                this.lastRevokingState = null; // not relevant if this event occurs
                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var partition in topicPartitionOffsets)
                    {
                        this.logger.LogInformation("[{0}] Partition revoked: {1}", this.configId, partition.ToString());
                    }
                }

                this.logger.LogTrace("[{0}] Calling Revoked event handler", this.configId);
                this.OnRevoked?.Invoke(this, new RevokedEventArgs(topicPartitionOffsets));
                this.logger.LogTrace("[{0}] Called Revoked event handler", this.configId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{0}] Exception occurred in PartitionsLostHandler", this.configId);
            }
        }

        private void PartitionsRevokedHandler(IConsumer<byte[]?, byte[]> consumer, List<TopicPartitionOffset> topicPartitionOffsets)
        {
            if (!ReferenceEquals(consumer, this.consumer)) return;
            SetAssignment(null);
            try
            {
                lastRevokingState = null;
                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var partition in topicPartitionOffsets)
                    {
                        this.logger.LogDebug("[{0}] Partition revoking: {1}", this.configId, partition.ToString());
                    }
                }

                this.OnRevoking?.Invoke(this, new RevokingEventArgs(topicPartitionOffsets));
                this.lastRevokingState = topicPartitionOffsets;
                revokeDeadline = UtcNow().AddMilliseconds(RevokeTimeoutPeriodInMs);
                var shouldInvoke = true;
                lastRevokeCancelAction = () =>
                {
                    shouldInvoke = false;
                    this.lastRevokeCancelAction = null;
                    this.lastRevokingState = null;
                    this.lastRevokeCompleteAction = null;
                    revokeDeadline = null;
                };
                lastRevokeCompleteAction = () =>
                {
                    if (!shouldInvoke) return;
                    this.lastRevokeCancelAction?.Invoke(); // clean itself up
                    if (this.logger.IsEnabled(LogLevel.Debug))
                    {
                        foreach (var partition in topicPartitionOffsets)
                        {
                            this.logger.LogDebug("[{0}] Partition revoked: {1}", this.configId, partition.ToString());
                        }
                    }
                    this.logger.LogTrace("[{0}] Calling Revoked event handler", this.configId);
                    this.OnRevoked?.Invoke(this, new RevokedEventArgs(topicPartitionOffsets));
                    this.logger.LogTrace("[{0}] Called Revoked event handler", this.configId);
                };
                if (closing || !EnableReAssignedLogic)
                {
                    lastRevokeCompleteAction();
                }
                else
                {
                    // Completed by the lifecycle owner on a later poll; never by a timer thread.
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{0}] Exception occurred in PartitionsRevokedHandler", this.configId);
            }
        }

        internal void PartitionsAssignedHandler(IConsumer<byte[]?, byte[]> consumer, List<TopicPartition> topicPartitions)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));
            if (!ReferenceEquals(consumer, this.consumer)) return;
            SetAssignment(topicPartitions);
            try
            {
                var lrs = this.lastRevokingState;
                this.lastRevokeCancelAction?.Invoke();

                var assignedPartitions = topicPartitions.ToList(); // Just in case source doesn't like us modifying this list
                if (lrs != null && this.OnRevoked != null)
                {
                    var sameTopicPartitions = topicPartitions
                        .Join(lrs, parti => parti, state => state.TopicPartition, (p, s) => s).ToList();
                    var sameAsPreviously = new List<TopicPartitionOffset>();
                    var seekFuncs = new List<ShouldSkipConsumeResult>();
                    foreach (var topicPartitionOffset in sameTopicPartitions)
                    {
                        var currentPosition = consumer.Position(topicPartitionOffset.TopicPartition);
                        if (currentPosition != topicPartitionOffset.Offset)
                        {
                            if (!topicPartitionOffset.Offset.IsSpecial)
                            {
                                seekFuncs.Add((cr) =>
                                {
                                    this.logger.LogDebug("[{0}] Seeking partition: {1}", this.configId, topicPartitionOffset.ToString());
                                    consumer.Seek(topicPartitionOffset);
                                    this.logger.LogDebug("[{0}] Seeked partition: {1}", this.configId, topicPartitionOffset.ToString());
                                    return cr.TopicPartition == topicPartitionOffset.TopicPartition && cr.Offset <= topicPartitionOffset.Offset;
                                });
                            }
                            else
                            {
                                continue; // Do not add it to same as previous
                            }
                        }
                        sameAsPreviously.Add(topicPartitionOffset);
                    }

                    if (seekFuncs.Count > 0)
                    {
                        seekFunc = (cr) =>
                        {
                            var skip = false;
                            foreach (var func in seekFuncs)
                            {
                                skip = func(cr) || skip; // order is important
                            }

                            seekFunc = (consRes) => false;
                            return skip;
                        }; // reset after seeking
                    }

                    var revoked = lrs.Except(sameAsPreviously).ToArray();

                    if (revoked.Length > 0)
                    {
                        if (this.logger.IsEnabled(LogLevel.Debug))
                        {
                            foreach (var partition in revoked)
                            {
                                this.logger.LogDebug("[{0}] Partition revoked: {1}", this.configId, partition.ToString());
                            }
                        }

                        if (this.OnRevoked != null)
                        {
                            this.logger.LogTrace("[{0}] Calling Revoked event handler", this.configId);
                            this.OnRevoked?.Invoke(this, new RevokedEventArgs(revoked));
                            this.logger.LogTrace("[{0}] Called Revoked event handler", this.configId);
                        }
                    }
                    if (this.logger.IsEnabled(LogLevel.Debug))
                    {
                        foreach (var partition in sameAsPreviously)
                        {
                            assignedPartitions.Remove(partition.TopicPartition);
                            this.logger.LogDebug("[{0}] Partition re-assigned: {1}", this.configId, partition.ToString());
                        }
                    }
                }
                
                if (this.logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var partition in assignedPartitions)
                    {
                        this.logger.LogDebug("[{0}] Partition Assigned: {1}", this.configId, partition.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{0}] Exception occurred in PartitionsAssignedHandler", this.configId);
            }
        }

        private void ConsumerStatisticsHandler(IConsumer<byte[]?, byte[]> consumer, string statisticsJson)
        {
            //
        }

        internal void ConsumerErrorHandler(IConsumer<byte[]?, byte[]> consumer, Error error)
        {
            this.OnErrorOccurredHandler(new KafkaException(error));
        }

        private void OnErrorOccurredHandler(KafkaException exception)
        {
            if (exception.Error.IsFatal) { Fail(exception.Error.Code.ToString(), "Fatal consumer error"); disconnected = true; }
            else if (IsOffsetInitializationFailure(exception.Error))
                RequestRecovery(exception.Error.Code.ToString(), "Committed offset initialization failed");

            if (exception.Message.ToLowerInvariant().Contains("disconnect"))
            {
                var match = Constants.ExceptionMsRegex.Match(exception.Message);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var ms))
                    {
                        if (ms > 180000)
                        {
                            this.logger.LogDebug(exception, "[{0}] Idle consumer connection reaped.", this.configId);
                            return;
                        }
                    }
                }
                this.logger.LogWarning(exception, "[{0}] Disconnected from kafka. Ignore unless occurs frequently in short period of time as client automatically reconnects.", this.configId);
                return;
            }
            
            if (exception.Message.Contains("Local: Maximum application poll interval (max.poll.interval.ms) exceeded"))
            {
                this.logger.LogWarning(exception, "[{0}] Processing of package took longer ({1}ms) than configured max.poll.interval.ms ({2}ms). Application was deemed dead/stuck and the consumer left the group so the assigned partitions could be assigned to a live application instance. Consider setting max.poll.interval.ms to a higher number if this a valid use case.", this.configId, Math.Round(currentPackageProcessTime.Elapsed.TotalMilliseconds), this.config.MaxPollIntervalMs ?? 300000);
                RequestRecovery(exception.Error.Code.ToString(), "Consumer disconnected");
                return;
            }
            
            if (exception.Message.Contains("Broker: Static consumer fenced by other consumer with same group.instance.id"))
            {
                this.logger.LogWarning(exception, "[{0}] Static consumer fenced by other consumer with same group.instance.id '{1}' in group '{2}'.", this.configId, this.config.GroupInstanceId, this.config.GroupId);
                RequestRecovery(exception.Error.Code.ToString(), "Consumer disconnected");
                return;
            }
            
            if (exception.Message.Contains("Broker: Invalid session timeout"))
            {
                this.logger.LogError(exception, "[{0}] Broker: Invalid session timeout {1}", this.configId, this.config.SessionTimeoutMs);
                this.disconnected = true;
                this.canReconnect = false;
                Fail(exception.Error.Code.ToString(), "Invalid consumer session timeout");
                return;
            }
            
            if (exception.Message.Contains("Receive failed") && exception.Message.Contains("timed out (after "))
            {
                var match = Constants.ExceptionMsRegex.Match(exception.Message);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var ms))
                    {
                        if (ms > 7500000)
                        {
                            this.logger.LogInformation(exception, "[{0}] Idle consumer connection timed out, Kafka will reconnect.", this.configId);
                            return;
                        }
                        this.logger.LogWarning(exception, "[{0}] Consumer connection timed out (after {1}ms in state UP). Kafka will reconnect.", this.configId, ms);
                        return;
                    }
                }
            }
            
            if (this.OnErrorOccurred == null)
            {
                this.logger.LogError(exception, "[{0}] Exception receiving from Kafka", this.configId);
            }
            else
            {
                // wrap error to include configId
                var wrappedError = new KafkaException(new Error(exception.Error.Code, $"[{this.configId}] {exception.Error.Reason}", exception.Error.IsFatal), exception.InnerException);
                NotifyError(wrappedError);
            }
        }

        /// <inheritdoc />
        public void Close()
        {
            Task? task;
            lock (lifecycleLock)
            {
                closing = true;
                workerTaskCts?.Cancel();
                task = workerTask;
            }
            if (ownerContext.Value) return;
            if (task != null && !task.Wait(recoveryPolicy.ShutdownTimeout))
            {
                Fail("ShutdownTimeout", "Lifecycle owner did not stop within shutdown budget");
                return; // Only the owner may dispose a native consumer, after Consume returns.
            }
            lock (lifecycleLock)
                if (ReferenceEquals(task, workerTask)) Publish(ConsumerLifecycleState.Stopped);
        }

        private async Task PollingWork(IConsumer<byte[]?, byte[]> native, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && !disconnected)
                {
                    try
                    {
                        var cr = native.Consume(TimeSpan.FromMilliseconds(100));
                        lock (diagnosticsLock) lastPoll = UtcNow();
                        if (cr != null)
                        {
                            if (!cr.IsPartitionEOF && seekFunc(cr)) continue;
                            lock (diagnosticsLock)
                            {
                                if (assignmentSettled) initialized.Add(cr.TopicPartition);
                                if (!cr.IsPartitionEOF) lastRecord = UtcNow();
                            }
                            ConfirmReady();
                            if (!cr.IsPartitionEOF)
                            {
                                currentPackageProcessTime.Restart();
                                await AddMessage(cr);
                                currentPackageProcessTime.Stop();
                            }
                        }
                        else ConfirmReady();
                        RefreshSnapshot();
                    }
                    catch (ConsumeException ex) { OnErrorOccurredHandler(ex); }
                    catch (OperationCanceledException) { }
                    if (revokeDeadline.HasValue && UtcNow() >= revokeDeadline.Value) lastRevokeCompleteAction?.Invoke();
                    if (Snapshot.State != ConsumerLifecycleState.Running && UtcNow() - initializationStarted >= recoveryPolicy.InitializationTimeout)
                        RequestRecovery("InitializationTimeout", "Assignment initialization deadline expired");
                    if (episodeStarted.HasValue && UtcNow() - episodeStarted.Value >= recoveryPolicy.EpisodeTimeout)
                    { Fail("RecoveryTimeout", "Recovery episode budget exhausted"); disconnected = true; }
                }
            }
            finally { workerTaskPollFinished?.TrySetResult(null); }
        }

        /// <inheritdoc />
        public void Commit()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException($"[{this.configId}] Unable to commits offset when disposed.");
            }

            lock (this.consumerLock)
            {
                if (this.consumer == null)
                {
                    throw new InvalidOperationException($"[{this.configId}] Unable to commit offsets when consumer is not open.");
                }

                try
                {
                    var positions = new List<TopicPartitionOffset>();
                    foreach (var topicPartition in this.consumer.Assignment)
                    {
                        var position = this.consumer.Position(topicPartition);
                        if (position != Offset.Unset)
                        {
                            positions.Add(new TopicPartitionOffset(topicPartition, position));
                        }
                    }
                    this.OnCommitting?.Invoke(this, new CommittingEventArgs(positions));
                    var committed = this.consumer.Commit();
                    this.OnCommitted?.Invoke(this, new CommittedEventArgs(GetCommittedOffsets(committed, null)));
                }
                catch (TopicPartitionOffsetException ex)
                {
                    this.OnCommitted?.Invoke(this, new CommittedEventArgs(GetCommittedOffsets(null, ex)));
                }
            }
        }
        
        /// <inheritdoc/>
        public void Commit(ICollection<TopicPartitionOffset> partitionOffsets)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException($"[{this.configId}] Unable to commits offset when disposed.");
            }

            lock (this.consumerLock)
            {
                if (this.consumer == null)
                {
                    throw new InvalidOperationException($"[{this.configId}] Unable to commit offsets when consumer is not open.");
                }

                if (!this.consumerGroupSet)
                {
                    logger.LogWarning("[{0}] Unable to commit without consumer group.", this.configId);
                    return;
                }

                
                List<string> invalidTopics;
                if (this.consumerTopicConfiguration.Topics != null)
                {
                    invalidTopics = partitionOffsets.Where(x => !this.consumerTopicConfiguration.Topics.Contains(x.Topic)).Select(x => x.Topic).Distinct().ToList();
                }
                else
                {
                    Debug.Assert(this.consumerTopicConfiguration.Partitions != null, "Partitions must not be null");
                    invalidTopics = partitionOffsets.Where(x => this.consumerTopicConfiguration.Partitions.All(y => y.TopicPartition != x.TopicPartition)).Select(x => x.Topic).Distinct().ToList();
                }
                
                if (invalidTopics.Count > 0)
                {
                    if (invalidTopics.Count == 0) throw new InvalidOperationException($"[{this.configId}] Topic {invalidTopics[0]} offset cannot be committed because topic is not subscribed to.");
                    throw new InvalidOperationException($"[{this.configId}] Topic {string.Join(", ", invalidTopics)} offsets cannot be committed because topics are not subscribed to.");
                }
                
                var latestPartitionOffsets =  partitionOffsets.GroupBy(y => y.TopicPartition).Select(y => y.OrderByDescending(z => z.Offset.Value).First()).ToList();

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    foreach (var topicPartitionOffset in latestPartitionOffsets)
                    {
                        logger.LogTrace("[{0}] Committing offset {1} for Topic {2}, Partition {3}", this.configId, topicPartitionOffset.Offset, topicPartitionOffset.Topic, topicPartitionOffset.Partition);
                    }
                }

                // TODO maybe validation on given partitions? Difficult, but possible once PartitionsAssignedHandler and PartitionsRevokedHandler is properly implemented
                try
                {
                    this.OnCommitting?.Invoke(this, new CommittingEventArgs(latestPartitionOffsets));
                    // When committing to kafka, one has to commit with one higher offset, see https://github.com/confluentinc/confluent-kafka-dotnet/blob/d5d85ea862581381fb318cdcff29e61a5cc72f8d/src/Confluent.Kafka/Consumer.cs#L459
                    var offsets = latestPartitionOffsets.Select(y => new TopicPartitionOffset(y.TopicPartition, y.Offset + 1)).ToList();
                    this.consumer.Commit(offsets);
                    this.OnCommitted?.Invoke(this, new CommittedEventArgs(GetCommittedOffsets(latestPartitionOffsets, null)));
                }
                catch (TopicPartitionOffsetException ex)
                {
                    this.OnCommitted?.Invoke(this, new CommittedEventArgs(GetCommittedOffsets(null, ex)));
                }
            }
        }


        private CommittedOffsets GetCommittedOffsets(ICollection<TopicPartitionOffset>? offsets, TopicPartitionOffsetException? ex)
        {
            if (offsets != null)
            {
                return new CommittedOffsets(offsets.Select(y => new TopicPartitionOffsetError(y, new Error(ErrorCode.NoError))).ToList(), new Error(ErrorCode.NoError));      
            }

            if (ex == null) throw new ArgumentNullException(nameof(ex), "Must provide either offset or exception");

            return new CommittedOffsets(ex.Results, ex.Error);
        }


        private async Task AddMessage(ConsumeResult<byte[]?, byte[]> result)
        {
            var args = new KafkaMessage(result);
            try
            {
                if (this.checkForKeepAlivePackets && args.IsKeepAliveMessage())
                {
                    this.logger.LogTrace("[{0}] KafkaConsumer: keep alive message read, ignoring.", this.configId);
                    return;
                }

                this.logger.LogTrace("[{0}] KafkaConsumer: raising MessageReceived", this.configId);
                var task = this.OnMessageReceived?.Invoke(args) ?? Task.CompletedTask;
                await task;
                this.logger.LogTrace("[{0}] KafkaConsumer: raised MessageReceived", this.configId);
            }
            catch (Exception ex)
            {
                if (this.OnErrorOccurred == null)
                {
                    this.logger.LogError(ex, "[{0}] Exception processing message read from Kafka", this.configId);
                }
                else
                {
                    NotifyError(ex);
                }
            }
        }

        /// <inheritdocs/>
        public void Dispose()
        {
            this.disposed = true;
            this.Close();
        }

        
        private delegate bool ShouldSkipConsumeResult(ConsumeResult<byte[]?, byte[]> consumeResult);
    }
}
