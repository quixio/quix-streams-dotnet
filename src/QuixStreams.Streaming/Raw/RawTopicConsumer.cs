using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka;
using QuixStreams.Telemetry.Kafka;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;

namespace QuixStreams.Streaming.Raw
{
    /// <summary>
    /// Topic class to read incoming raw messages (capable to read non-quixstreams messages)
    /// </summary>
    public class RawTopicConsumer: IRawTopicConsumer, IDisposable
    {
        private readonly string topicName;
        private IKafkaConsumer kafkaConsumer;
        private bool connectionStarted = false;

        EventHandler<Exception> _errorHandler;
        bool errorHandlerRegistered = false;

        /// <inheritdoc />
        public event EventHandler<KafkaMessage> OnMessageReceived;
        
        /// <inheritdoc />
        public event EventHandler OnDisposed;

        /// <inheritdoc />
        public event EventHandler<Exception> OnErrorOccurred
        {
            add {
                _errorHandler += value;
                if (_errorHandler != null && !errorHandlerRegistered)
                {
                    //automatic attaching the handler when noone someone starts listening to the event
                    // internally causing to stop logging messages instead of start throwing them
                    this.kafkaConsumer.OnErrorOccurred += InternalErrorHandler;
                    errorHandlerRegistered = true;
                }
            }
            remove { 
                _errorHandler -= value;
                if (_errorHandler == null && errorHandlerRegistered)
                {
                    //automatic detaching of the handler when noone is listening to the event
                    // internally causing to start logging messages instead of throwing them
                    this.kafkaConsumer.OnErrorOccurred -= InternalErrorHandler;
                    errorHandlerRegistered = false;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicConsumer"/>
        /// </summary>
        /// <param name="brokerAddress">Address of Kafka cluster.</param>
        /// <param name="topicName">Name of the topic.</param>
        /// <param name="consumerGroup">The consumer group id to use for consuming messages. If null, consumer group is not used and only consuming new messages.</param>
        /// <param name="brokerProperties">Additional broker properties</param>
        /// <param name="autoOffset">The offset to use when there is no saved offset for the consumer group.</param>
        /// <param name="partitions">The partitions to subscribe to. If not provided, All partitions are subscribed to according to other configuration such as consumer group.</param>
        public RawTopicConsumer(string brokerAddress, string topicName, string consumerGroup, Dictionary<string, string> brokerProperties = null, AutoOffsetReset? autoOffset = null, ICollection<Partition> partitions = null)
        {
            this.topicName = topicName;
            brokerProperties ??= new Dictionary<string, string>();
            if (!brokerProperties.ContainsKey("fetch.message.max.bytes")) brokerProperties["fetch.message.max.bytes"] = "20480";

            var consConfig = new ConsumerConfiguration(brokerAddress, consumerGroup, brokerProperties);

            if (autoOffset != null)
            {
                consConfig.AutoOffsetReset = autoOffset?.ConvertToKafka();
            }

            //disable quix-custom keep alive messages because they can interfere with the received data since we dont have any protocol running this over
            consConfig.CheckForKeepAlivePackets = false;

            var topicConfiguration = new ConsumerTopicConfiguration(topicName, partitions);
            this.kafkaConsumer = new KafkaConsumer(consConfig, topicConfiguration);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RawTopicConsumer"/>
        /// </summary>
        /// <param name="kafkaConsumer">The kafka consumer to use</param>
        /// <param name="topicName">The optional topic name to use</param>
        public RawTopicConsumer(IKafkaConsumer kafkaConsumer, string topicName = null)
        {
            this.topicName = topicName ?? "Unknown";
            this.kafkaConsumer = kafkaConsumer;
        }
        

        /// <inheritdoc />
        public void Subscribe()
        {
            if (connectionStarted)
            {
                var logger = Logging.CreateLogger<RawTopicConsumer>();
                logger.LogWarning("Attempted to subscribe to topic {0} more than once.", this.topicName);
                return;
            }

            kafkaConsumer.OnMessageReceived = OnNewMessageReceivedHandler;

            kafkaConsumer.Open();
            connectionStarted = true;
        }

        private Task OnNewMessageReceivedHandler(KafkaMessage kafkaMessage)
        {
            this.OnMessageReceived?.Invoke(this, kafkaMessage);
            return Task.CompletedTask;
        }
        
        /// <inheritdoc />
        public void Unsubscribe()
        {
            if (!connectionStarted) return;

            kafkaConsumer.Close();
            kafkaConsumer.OnMessageReceived = null;
            connectionStarted = false;
        } 

        /// <summary>
        /// Internal handler for handing Error event from the kafkaOutput
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ex"></param>
        void InternalErrorHandler(object source, Exception ex)
        {
            this._errorHandler?.Invoke(this, ex);
        }


        /// <inheritdoc />
        public void Dispose()
        {
            this.kafkaConsumer?.Dispose();
            this.OnDisposed?.Invoke(this, EventArgs.Empty);
        }

    }
}