﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace QuixStreams.Kafka
{
    /// <summary>
    /// Kafka producer implemented using Queueing mechanism
    /// </summary>
    public class KafkaProducer : IKafkaProducer
    {
        private readonly ProducerTopicConfiguration topicConfiguration;
        private readonly ProducerConfig config;

        private readonly object flushLock = new object();
        private readonly object sendLock = new object();

        private readonly ILogger logger = Logging.CreateLogger<KafkaProducer>();
        private IDictionary<string, string> brokerStates = new Dictionary<string, string>();
        private bool checkBrokerStateBeforeSend = false;
        private bool logOnNextBrokerStateUp = false;
        private bool disableKafkaLogsByBrokerLogWorkaround = false; // if enabled, no actual kafka logs should be shown
        private string lastReportedBrokerDownMessage = string.Empty;
        
        /// <summary>
        /// Due to differing framing overhead between protocol versions the producer is unable
        /// to reliably enforce a strict max message limit at produce time.
        /// We introduce an extra buffer on top of Kafka's reported max message size in order to limit exposure to
        /// potential issues.
        /// The value was picked after testing to be the difference between librdkafka message size and default broker
        /// message size limit 
        /// </summary>
        private const int MaxMessageSizePadding = 48588;
        
        /// <summary>
        /// As per https://github.com/confluentinc/librdkafka/blob/master/CONFIGURATION.md message.max.bytes
        /// </summary>
        private const int DefaultMaxMessageSize = 1000000;

        private ProducerDelegate internalProducer;
        private Dictionary<string, int> topicPartitionCount = new Dictionary<string, int>();

        private long lastFlush = -1;
        private IProducer<byte[]?, byte[]> producer;
        private string configId;
        private readonly ProducerConfiguration producerConfiguration;

        /// <summary>
        /// Initializes a new instance of <see cref="KafkaProducer"/>
        /// </summary>
        /// <param name="producerConfiguration">The publisher configuration</param>
        /// <param name="topicConfiguration">The topic configuration</param>
        public KafkaProducer(ProducerConfiguration producerConfiguration, ProducerTopicConfiguration topicConfiguration)
        {
            this.topicConfiguration = topicConfiguration;
            this.config = this.GetKafkaProducerConfig(producerConfiguration);
            this.producerConfiguration = producerConfiguration;
            this.configId = CreateConfigId(topicConfiguration);
            this.producer = CreateProducer();
            this.internalProducer = CreateInternalProducer();

            // Helpers
            ProducerDelegate CreateInternalProducer()
            {
                // When we have a delegate to decide the partition
                if (topicConfiguration.Partitioner != null)
                {
                    return (msg, handler, _) =>
                    {
                        if (this.disposed) throw new ObjectDisposedException("Producer is already disposed");
                        var maxPartition = GetPartitionCount(topicConfiguration.Topic);
                        var partition = topicConfiguration.Partitioner(topicConfiguration.Topic, maxPartition, msg);
                        this.producer.Produce(new TopicPartition(topicConfiguration.Topic, partition),
                            new Message<byte[]?, byte[]>
                            {
                                Key = msg.Key,
                                Value = msg.Value,
                                Headers = msg.ConfluentHeaders,
                                Timestamp = (Timestamp)msg.Timestamp
                            }, handler);
                    };
                }

                // When Any partition is good
                if (topicConfiguration.Partition == Partition.Any)
                {
                    return (msg, handler, _) =>
                    {
                        if (this.disposed) throw new ObjectDisposedException("Producer is already disposed");
                        this.producer.Produce(topicConfiguration.Topic,
                            new Message<byte[]?, byte[]>
                            {
                                Key = msg.Key,
                                Value = msg.Value,
                                Headers = msg.ConfluentHeaders,
                                Timestamp = (Timestamp)msg.Timestamp
                            }, handler);
                    };
                }

                // When must be a specific partition
                var topicPartition = new TopicPartition(topicConfiguration.Topic, topicConfiguration.Partition);
                return (msg, handler, _) =>
                {
                    if (this.disposed) throw new ObjectDisposedException("Producer is already disposed");
                    this.producer.Produce(topicPartition,
                        new Message<byte[]?, byte[]>
                        {
                            Key = msg.Key,
                            Value = msg.Value,
                            Headers = msg.ConfluentHeaders,
                            Timestamp = (Timestamp)msg.Timestamp
                        }, handler);
                };
            }
            
                    
            string CreateConfigId(ProducerTopicConfiguration topicConfiguration)
            {
                var configId = Guid.NewGuid().GetHashCode().ToString("X8");
                var logBuilder = new StringBuilder();
                logBuilder.AppendLine();
                logBuilder.AppendLine("=================== Kafka Producer Configuration =====================");
                logBuilder.AppendLine("= Configuration Id: " + configId);
                logBuilder.AppendLine(topicConfiguration.Partitioner != null
                    ? $"= Topic: {topicConfiguration.Topic} with partitioner"
                    : $"= Topic: {topicConfiguration.Topic}{topicConfiguration.Partition}");
                foreach (var keyValuePair in this.config)
                {
                    if (keyValuePair.Key?.IndexOf("password", StringComparison.InvariantCultureIgnoreCase) > -1 ||
                        keyValuePair.Key?.IndexOf("username", StringComparison.InvariantCultureIgnoreCase) > -1 ||
                        keyValuePair.Key?.IndexOf("ssl.ca.pem", StringComparison.InvariantCultureIgnoreCase) > -1)
                    {
                        logBuilder.AppendLine($"= {keyValuePair.Key}: [REDACTED]");
                    }
                    else logBuilder.AppendLine($"= {keyValuePair.Key}: {keyValuePair.Value}");
                }
                logBuilder.Append("======================================================================");
                this.logger.LogDebug(logBuilder.ToString());
                return configId;
            }

            IProducer<byte[]?, byte[]> CreateProducer()
            {
                lastReportedBrokerDownMessage = string.Empty;
                checkBrokerStateBeforeSend = false;

                var builder = new ProducerBuilder<byte[]?, byte[]>(this.config)
                    .SetErrorHandler(this.ErrorHandler)
                    .SetLogHandler(this.ProducerLogHandler);
                return builder.Build();
            }
        }

        private ProducerConfig GetKafkaProducerConfig(ProducerConfiguration producerConfiguration)
        {
            var config = producerConfiguration.ToProducerConfig();
            if (string.IsNullOrWhiteSpace(config.DeliveryReportFields))
            {
                config.DeliveryReportFields = "none";
            }

            config.Debug = config.Debug;
            if (!string.IsNullOrWhiteSpace(config.Debug))
            {
                if (config.Debug.Contains("all")) return config;
                if (config.Debug.Contains("broker")) return config;
                // There is a debug configuration other than all or queue
                this.logger.LogDebug("In order to enable a workaround to check if broker is up, additional broker logs will be visible");
                config.Debug = (config.Debug.TrimEnd(new[] { ',', ' ' }) + ",broker").TrimStart(',');
                return config;
            }

            disableKafkaLogsByBrokerLogWorkaround = true;
            config.Debug = "broker";

            return config;
        }

        private int GetPartitionCount(string topic)
        {
            if (this.topicPartitionCount.TryGetValue(topic, out var partitionCount)) return partitionCount;
            lock (this.topicPartitionCount)
            {
                if (this.topicPartitionCount.TryGetValue(topic, out partitionCount)) return partitionCount;

                this.logger.LogTrace("[{0}] Creating admin client to retrieve partition count for topic {1}", this.configId, topic);

                void NullLoggerForAdminLogs(IAdminClient adminClient, LogMessage logMessage)
                {
                    // Log nothing
                }

                using var adminClient = GetAdminClientBuilder(this.config).SetLogHandler(NullLoggerForAdminLogs).Build();

                var metadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
                if (metadata == null)
                {
                    throw new OperationCanceledException($"[{this.configId}] Topic '{topic}' metadata timed out while retrieving maximum partition count"); // Maybe a more specific exception ?
                }

                this.logger.LogTrace("[{0}] Retrieved metadata for topic {1}", this.configId, topic);
                var topicMetaData = metadata.Topics.FirstOrDefault(x => x.Topic == topic);
                if (topicMetaData == null)
                {
                    throw new Exception($"[{this.configId}] Failed to retrieve metadata for topic '{topic}' to determine maximum partition count."); // Maybe a more specific exception ?
                }

                if (topicMetaData.Partitions.Count == 0)
                {
                    throw new OperationCanceledException($"[{this.configId}] Found no partition information for topic '{topic}'. The topic may not exist or lacking permission to use it"); // Maybe a more specific exception ?
                }

                this.topicPartitionCount[topic] = topicMetaData.Partitions.Count;
                return topicMetaData.Partitions.Count;
            }
        }

        private AdminClientBuilder GetAdminClientBuilder(ProducerConfig config)
        {
            var filteredConfig = config.Where(prop =>
                    !prop.Key.StartsWith("dotnet.producer.") &&
                    !prop.Key.StartsWith("dotnet.consumer."))
                .ToDictionary(k => k.Key, v => v.Value);
            
            var adminConfig = new ProducerConfig(filteredConfig);

            return new AdminClientBuilder(adminConfig);
        }

        private async Task UpdateMaxMessageSize(TimeSpan maxWait)
        {
            var max = DateTime.UtcNow.Add(maxWait);
            try
            {
                void NullLoggerForAdminLogs(IAdminClient adminClient, LogMessage logMessage)
                {
                    // Log nothing
                }
                
                using (var adminClient = GetAdminClientBuilder(this.config).SetLogHandler(NullLoggerForAdminLogs).Build())
                {
                    var maxRequestTimeTopic = max - DateTime.UtcNow;
                    var topicConfig = await adminClient.DescribeConfigsAsync(new ConfigResource[]
                            { new ConfigResource() { Type = ResourceType.Topic, Name = topicConfiguration.Topic } },
                        new DescribeConfigsOptions()
                        {
                            RequestTimeout = maxRequestTimeTopic
                        });
                    
                    var maxBrokerMessageBytesNumeric = int.MaxValue;
                    try
                    {
                        var maxRequestTimeBroker = max - DateTime.UtcNow;
                        if (maxRequestTimeBroker > TimeSpan.FromSeconds(0)) 
                        {
                            var brokerConfig = await adminClient.DescribeConfigsAsync(new ConfigResource[]
                                    { new ConfigResource() { Type = ResourceType.Broker, Name = "0" } },
                                new DescribeConfigsOptions()
                                {
                                    RequestTimeout = maxRequestTimeBroker
                                });

                            if (brokerConfig.FirstOrDefault()?.Entries
                                    .TryGetValue("message.max.bytes", out var maxBrokerMessageBytes) == true &&
                                int.TryParse(maxBrokerMessageBytes.Value, out maxBrokerMessageBytesNumeric))
                            {
                                if (maxBrokerMessageBytesNumeric > MaxMessageSizePadding * 2)
                                {
                                    maxBrokerMessageBytesNumeric -= MaxMessageSizePadding;
                                }
                                else
                                {
                                    // This is a hope that by halving, it'll be able to get packed, but no guarantee
                                    // removing more than this seem counter intuitive?
                                    maxBrokerMessageBytesNumeric = (int)Math.Ceiling(maxBrokerMessageBytesNumeric * 0.5);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // no necessary permissions/timeout?
                    }

                    if (topicConfig.FirstOrDefault()?.Entries
                            .TryGetValue("max.message.bytes", out var maxTopicMessageBytes) != true || maxTopicMessageBytes == null ||
                        !int.TryParse(maxTopicMessageBytes.Value, out var maxTopicMessageBytesNumeric))
                    {
                        producerConfiguration.MessageMaxBytes = Math.Min(DefaultMaxMessageSize, maxBrokerMessageBytesNumeric);
                    }
                    else
                    {
                        if (maxBrokerMessageBytesNumeric < maxTopicMessageBytesNumeric)
                        {
                            this.logger.LogDebug(
                                "[{0}] Broker max message size {1} is less than topic max message size {2}, using broker at upper limit",
                                this.configId,
                                maxBrokerMessageBytesNumeric, maxTopicMessageBytesNumeric);
                            producerConfiguration.MessageMaxBytes = maxBrokerMessageBytesNumeric;
                        }
                        else
                        {
                            if (maxTopicMessageBytesNumeric > MaxMessageSizePadding * 5)
                            {
                                producerConfiguration.MessageMaxBytes =
                                    maxTopicMessageBytesNumeric - MaxMessageSizePadding;
                            }
                            else
                            {
                                producerConfiguration.MessageMaxBytes = (int)Math.Ceiling(maxTopicMessageBytesNumeric * 0.5);
                            }
                        }
                    }
                }

                this.logger.LogDebug("[{0}] Maximum message size for the topic is {1} bytes", this.configId, producerConfiguration.MessageMaxBytes);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "[{0}] Failed to get maximum message size from topic, will default to 1MB", this.configId);
                producerConfiguration.MessageMaxBytes = DefaultMaxMessageSize;
            }
        }
        
        /// <inheritdoc />
        public async Task<int> GetMaxMessageSizeBytes(TimeSpan maxWait)
        {
            if (this.producerConfiguration.MessageMaxBytes != null)
            {
                return producerConfiguration.MessageMaxBytes!.Value - 1;
            };

            lock (this.producerConfiguration)
            {
                if (this.producerConfiguration.MessageMaxBytes != null)
                {
                    return producerConfiguration.MessageMaxBytes!.Value - 1;
                }
            }

            await this.UpdateMaxMessageSize(maxWait);
            return this.producerConfiguration.MessageMaxBytes!.Value - 1;
        }

        private void ProducerLogHandler(IProducer<byte[]?, byte[]> producer, LogMessage msg)
        {
            if (KafkaHelper.TryParseBrokerNameChange(msg, out var oldName, out var newName))
            {
                if (brokerStates.ContainsKey(oldName))
                {
                    brokerStates[newName] = brokerStates[oldName];
                    brokerStates.Remove(oldName);
                    if (disableKafkaLogsByBrokerLogWorkaround) this.logger.LogTrace("[{0}] Broker {1} is now {2}", this.configId, oldName, newName);
                }
            }
            
            if (KafkaHelper.TryParseBrokerState(msg, out var broker, out var state))
            {
                if (logOnNextBrokerStateUp && state.Equals("up", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.logger.LogInformation("[{0}] Broker {1} is now {2}", this.configId, broker, state);
                } 
                brokerStates[broker] = state;
            }
            
            if (disableKafkaLogsByBrokerLogWorkaround) return;
            
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

        private void ErrorHandler(IProducer<byte[]?, byte[]> producer, Error error)
        {
            // TODO possibly allow delegation of error up
            var ex = new KafkaException(error);
            if (ex.Message.ToLowerInvariant().Contains("disconnect"))
            {
                var match = Constants.ExceptionMsRegex.Match(ex.Message);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var ms))
                    {
                        if (ms > 180000)
                        {
                            this.logger.LogDebug(ex, "[{0}] Idle producer connection reaped.", this.configId);
                            return;
                        }
                    }
                }
                this.logger.LogWarning(ex, "[{0}] Disconnected from kafka. Ignore unless occurs frequently in short period of time as client automatically reconnects.", this.configId);
                return;
            }

            if (ex.Message.Contains("brokers are down"))
            {
                if (!checkBrokerStateBeforeSend ||
                    ex.Message != lastReportedBrokerDownMessage)
                {
                    checkBrokerStateBeforeSend = true;
                    lastReportedBrokerDownMessage = ex.Message;
                    this.logger.LogDebug(
                        "[{0}] {1}, but delaying reporting until next message, in case reconnect happens before.",
                        this.configId, ex.Message); // Excessive error reporting
                }

                return;
            }
            
            if (ex.Message.Contains("Receive failed") && ex.Message.Contains("timed out (after "))
            {
                var match = Constants.ExceptionMsRegex.Match(ex.Message);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var ms))
                    {
                        if (ms > 7500000)
                        {
                            this.logger.LogInformation(ex, "[{0}] Idle producer connection timed out, Kafka will reconnect.", this.configId);
                            return;
                        }
                        this.logger.LogWarning(ex, "[{0}] Producer connection timed out (after {1}ms in state UP). Kafka will reconnect.", this.configId, ms);
                        return;
                    }
                }
            }
            
            this.logger.LogError(ex, "[{0}] Kafka producer exception", this.configId);
        }

        /// <inheritdoc/>
        public Task Publish(KafkaMessage message, CancellationToken cancellationToken = default)
        {
            return this.SendInternal(message, this.internalProducer, cancellationToken);
        }

        /// <inheritdoc/>
        public Task Publish(IEnumerable<KafkaMessage> messages, CancellationToken cancellationToken = default)
        {
            var lastTask = Task.CompletedTask;
            lock (this.sendLock)
            {
                foreach (var kafkaMessage in messages)
                {
                    if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
                    lastTask = this.SendInternal(kafkaMessage, this.internalProducer, cancellationToken);
                }
            }

            return lastTask;
        }


        private Task SendInternal(KafkaMessage message, ProducerDelegate handler,  CancellationToken cancellationToken = default, object? state = null)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
            if (this.disposed)
            {
                throw new ObjectDisposedException($"[{this.configId}] Unable to write as producer is disposed");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DeliveryResult<byte[], byte[]>>(cancellationToken);
            }

            var taskSource = new TaskCompletionSource<TopicPartitionOffset>(TaskCreationOptions.RunContinuationsAsynchronously);

            void DeliveryHandler(DeliveryReport<byte[]?, byte[]> report)
            {
                if (report.Error?.IsError == true)
                {
                    this.logger.LogTrace("[{0}] {1} {2}", this.configId, report.Error.Code, report.Error.Reason);
                    var wrappedError = new Error(report.Error.Code, $"[{this.configId}] {report.Error.Reason}", report.Error.IsFatal);
                    taskSource.SetException(new KafkaException(wrappedError));
                    return;
                }

                taskSource.SetResult(report.TopicPartitionOffset);
            }

            var success = false;
            var maxTry = 10;
            var tryCount = 0;
            lock (sendLock) // to avoid reordering of packages in case of error
            {
                if (checkBrokerStateBeforeSend)
                {
                    checkBrokerStateBeforeSend = false;
                    var upBrokerCount = this.brokerStates.Count(y => y.Value.Equals("up", StringComparison.InvariantCultureIgnoreCase));
                    if (upBrokerCount == 0)
                    {
                        logOnNextBrokerStateUp = true;
                        this.logger.LogError("[{0}] None of the brokers are currently in state 'up'.", this.configId);
                        if (this.logger.IsEnabled(LogLevel.Debug))
                        {
                            foreach (var brokerState in brokerStates)
                            {
                                this.logger.LogDebug("[{0}] Broker {1} has state {2}", this.configId, brokerState.Key, brokerState.Value);
                            }
                        }
                    }
                    else
                    {
                        this.logger.LogDebug("[{0}] At least {1}/{2} brokers are up (after all being marked down).", this.configId, upBrokerCount, this.brokerStates.Count);
                    }
                } 
                do
                {
                    try
                    {
                        tryCount++;
                        handler(message, DeliveryHandler, state);
                        success = true;
                    }
                    catch (ProduceException<byte[], byte[]> e)
                    {
                        if (tryCount == maxTry) throw;

                        if (e.Error.Code == ErrorCode.Local_QueueFull)
                        {
                            this.Flush(cancellationToken);
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (!success && tryCount <= maxTry);
            }

            return taskSource.Task;
        }

        /// <inheritdoc />
        public void Flush(CancellationToken cancellationToken)
        {
            if (this.disposed) throw new ObjectDisposedException($"[{this.configId}] Unable to flush a disposed " + nameof(KafkaProducer));
            var flushTime = DateTime.UtcNow.ToBinary();
            if (flushTime < this.lastFlush) return;

            lock (this.flushLock)
            {
                if (flushTime < this.lastFlush) return;

                try
                {
                    this.producer.Flush(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // someone cancelled token
                }
                catch (ObjectDisposedException)
                {
                    // the underlying producer is disposed
                }

                this.lastFlush = DateTime.UtcNow.ToBinary();
            }
        }

        
        
        private bool disposed = false;
        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;
            this.producer.Dispose();
            disposed = true;
        }

        private delegate void ProducerDelegate(KafkaMessage message, Action<DeliveryReport<byte[]?, byte[]>> deliveryHandler, object? state);
    }
}