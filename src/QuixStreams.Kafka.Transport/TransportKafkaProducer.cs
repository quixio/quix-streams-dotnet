using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using QuixStreams.Kafka.Transport.SerDes;

namespace QuixStreams.Kafka.Transport
{
    /// <summary>
    /// The interface required to implement an <see cref="IProducer{TKey,TValue}"/>, which sends <see cref="Package"/> to Kafka
    /// </summary>
    public interface IKafkaTransportProducer: IDisposable
    {
        /// <summary>
        /// Publishes a package
        /// </summary>
        /// <param name="package">The package to publish</param>
        /// <param name="cancellationToken">The cancellation token to listen to for aborting the process</param>
        Task Publish(TransportPackage package, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Flush the queue to Kafka
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for aborting flushing</param>
        Task Flush(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// A prebuilt pipeline, which serializes and optionally splits the provided packages then passes into the specified producer.
    /// </summary>
    public class KafkaTransportProducer : IKafkaTransportProducer
    {
        private readonly IPackageSerializer packageSerializer;
        private IKafkaMessageSplitter kafkaMessageSplitter;
        private readonly IKafkaProducer producer;
        private Task lastPublishTask = null;
        private object msgSplitLock = new object();

        /// <summary>
        /// Initializes a new instance of <see cref="KafkaTransportProducer"/> with the specified <see cref="IProducer{TKey,TValue}"/>
        /// </summary>
        /// <param name="producer">The producer to pass the serialized packages into</param>
        /// <param name="packageSerializer">The package serializer to use</param>
        /// <param name="kafkaMessageSplitter">The optional byte splitter to use. When not provided, one may be created if splitting is enabled based on producer settings</param>
        public KafkaTransportProducer(IKafkaProducer producer, IPackageSerializer packageSerializer = null, IKafkaMessageSplitter kafkaMessageSplitter = null)
        {
            this.producer = producer ?? throw new ArgumentNullException(nameof(producer));
            this.packageSerializer = packageSerializer ?? new PackageSerializer();
            this.kafkaMessageSplitter = kafkaMessageSplitter;
        }

        /// <inheritdocs/>
        public Task Publish(TransportPackage transportPackage, CancellationToken cancellationToken = default)
        {
            // this -> serializer -?> byteSplitter -> producer
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            var serialized = this.packageSerializer.Serialize(transportPackage);

            if (PackageSerializationSettings.EnableMessageSplit)
            {
                if (this.kafkaMessageSplitter == null)
                {
                    lock (this.msgSplitLock)
                    {
                        // attempts to make it proper async were in wain after several variants
                        // ideas are welcome, any attempt so far resulted in intermittent test fails
                        // but this worked
                        var size = this.producer
                            .GetMaxMessageSizeBytes(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

                        if (size > 1000) size -= 1000; // This is done to offset kafka message overhead causing Message too Large exceptions
                        this.kafkaMessageSplitter = new KafkaMessageSplitter(size);
                    }
                }
                
                if (this.kafkaMessageSplitter.ShouldSplit(serialized))
                {
                    var splitMessages = this.kafkaMessageSplitter.Split(serialized);
                    return this.lastPublishTask = this.producer.Publish(splitMessages, cancellationToken);
                }
            }

            return this.lastPublishTask = this.producer.Publish(serialized, cancellationToken);
        }

        
        /// <inheritdocs/>
        public Task Flush(CancellationToken cancellationToken = default)
        {
            return this.lastPublishTask ?? Task.CompletedTask;
        }

        /// <inheritdocs/>
        public void Dispose()
        {
            try
            {
                this.Flush().GetAwaiter().GetResult();
            }
            catch (ProducerClosedException)
            {
                // ignore
            }
        }
    }
}