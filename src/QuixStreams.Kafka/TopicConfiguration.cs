using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Confluent.Kafka;

namespace QuixStreams.Kafka
{
    /// <summary>
    /// Topic configuration for Kafka producer
    /// </summary>
    public sealed class ProducerTopicConfiguration
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProducerTopicConfiguration"/>
        /// </summary>
        /// <param name="topic">The topic to write to</param>
        /// <param name="partition">The partition to write to</param>
        public ProducerTopicConfiguration(string topic, Partition partition)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentOutOfRangeException(nameof(topic), "Topic cannot be null or empty");
            }

            this.Topic = topic;
            this.Partition = partition;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ProducerTopicConfiguration"/>
        /// </summary>
        /// <param name="topic">The topic to write to</param>
        public ProducerTopicConfiguration(string topic) : this(topic, Partition.Any)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ProducerTopicConfiguration"/>
        /// </summary>
        /// <param name="topic">The topic to write to</param>
        /// <param name="partitioner">The partitioner to use when sending a message</param>
        public ProducerTopicConfiguration(string topic, QuixPartitionerDelegate partitioner) : this(topic, Partition.Any)
        {
            Partitioner = partitioner ?? throw new ArgumentOutOfRangeException(nameof(partitioner), "Partitioner cannot be null or empty");
        }

        /// <summary>
        /// The topic to write to
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// The partition to write to
        /// </summary>
        public Partition Partition { get; }
        
        /// <summary>
        /// The partition to select the partition for the message
        /// </summary>
        public QuixPartitionerDelegate? Partitioner { get; }
    }

    public sealed class ConsumerTopicConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> for a single topic where partitions will be automatically 
        /// selected and offset will be the last unread offset or first available offset if
        /// no previous offset for consumer group is found.
        /// </summary>
        /// <param name="topic">The topic</param>
        public ConsumerTopicConfiguration(string topic)
        {
            this.Topics = new ReadOnlyCollection<string>(new List<string>(1) {topic});
        }

        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> for one or more topics where partitions will be automatically 
        /// selected and offset will be the last unread offset or first available
        /// offset if no previous offset for consumer group is found.
        /// </summary>
        /// <param name="topics">The topics</param>
        public ConsumerTopicConfiguration(ICollection<string> topics)
        {
            this.Topics = new ReadOnlyCollection<string>(topics.ToList());
        }

        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic and partition with default offset. 
        /// Default is the last unread offset or first available offset if
        /// no previous offset for consumer group is found.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partition">The partition to set the offset to default for</param>
        public ConsumerTopicConfiguration(string topic, Partition partition) : this(topic, new List<Partition>(1) {partition}, Offset.Unset)
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic and offset with default partition.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="offset">The offset to use</param>
        public ConsumerTopicConfiguration(string topic, Offset offset) : this(topic, new List<Partition>(1) {Partition.Any}, offset)
        {
        }        

        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic and partition with the specified offset.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partition">The partition to set the offset for</param>
        /// <param name="offset">The offset</param>
        public ConsumerTopicConfiguration(string topic, Partition partition, Offset offset) : this(topic, new List<Partition>(1) {partition}, offset)
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic and partition with the specified offset.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partitionOffset">The partition offset to use</param>
        public ConsumerTopicConfiguration(string topic, PartitionOffset partitionOffset) : this(topic, partitionOffset?.Partition, partitionOffset?.Offset)
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic and partition with the specified offset.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partition">The partition to set the offset for</param>
        /// <param name="offset">The offset</param>
        public ConsumerTopicConfiguration(string topic, Partition? partition, Offset? offset) : this(topic, new List<Partition>(1) { partition ?? Partition.Any }, offset ?? Offset.Unset)
        {
        }
        
        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic for, which all specified partitions are set to default. 
        /// Default is the last unread offset or first available offset if
        /// no previous offset for consumer group is found.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partitions">The partitions to set the offset to default for</param>
        public ConsumerTopicConfiguration(string topic, ICollection<Partition> partitions) : this(topic, partitions, Offset.Unset)
        {
        }


        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with multiple topics where each topic has one or more configured partition offset
        /// </summary>
        /// <param name="topicPartitionOffsets">The topics with partition offsets</param>
        public ConsumerTopicConfiguration(ICollection<TopicPartitionOffset> topicPartitionOffsets)
        {
            if (topicPartitionOffsets == null)
            {
                throw new ArgumentNullException(nameof(topicPartitionOffsets));
            }

            if (topicPartitionOffsets.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topicPartitionOffsets), "Cannot be empty");
            }

            var groupedTopicPartitions = topicPartitionOffsets.GroupBy(tpo => tpo.Topic).ToDictionary(x => x.Key, x => x.ToList());

            foreach (var partitionOffsets in groupedTopicPartitions)
            {
                if (partitionOffsets.Value.Any(x => x.Offset.Value.Equals(Partition.Any)) && partitionOffsets.Value.Count != 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(partitionOffsets),
                        $"Provided multiple partition values for {partitionOffsets.Key} where at least one is Any. Should provide only Any or multiple without Any.");
                }
            }

            // check if it is a simple subscription to topics
            if (topicPartitionOffsets.All(p => p.Partition == Partition.Any && p.Offset == Offset.Unset))
            {
                this.Topics = new ReadOnlyCollection<string>(topicPartitionOffsets.Select(x => x.Topic).Distinct().ToList());
                return;
            }
 
            // no, it is not a simple topic subscription
            this.Partitions = new ReadOnlyCollection<TopicPartitionOffset>(topicPartitionOffsets.ToList());
        }

        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic for, which all partitions set to one type of offset.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partitions">The partitions to set the offset for</param>
        /// <param name="offset">The offset</param>
        public ConsumerTopicConfiguration(string topic, ICollection<Partition> partitions, Offset offset) : this(topic, partitions?.Select(p => new PartitionOffset(p.Value, offset)).ToList() ?? new List<PartitionOffset> { new PartitionOffset(Partition.Any, Offset.Unset)})
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ConsumerTopicConfiguration" /> with a single topic with the specified partition offsets.
        /// </summary>
        /// <param name="topic">The topic to set the partitions for</param>
        /// <param name="partitionOffsets">The partitions with offsets to listen to</param>
        public ConsumerTopicConfiguration(string topic, ICollection<PartitionOffset> partitionOffsets) : this(partitionOffsets.Select(x=> new TopicPartitionOffset(topic, x.Partition, x.Offset)).ToList())
        {
        }

        /// <summary>
        /// The topics
        /// </summary>
        public IReadOnlyCollection<string>? Topics { get; }

        /// <summary>
        /// The topics with partition offsets
        /// </summary>
        public IReadOnlyCollection<TopicPartitionOffset>? Partitions { get; }
    }

    /// <summary>
    /// The offset to use for a given partition
    /// </summary>
    public class PartitionOffset
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PartitionOffset"/>
        /// </summary>
        /// <param name="partition">The partition</param>
        /// <param name="offset">The offset</param>
        public PartitionOffset(Partition partition, Offset offset)
        {
            this.Partition = partition;
            this.Offset = offset;
        }

        /// <summary>
        /// The partition
        /// </summary>
        public Partition Partition { get; }

        /// <summary>
        /// The Offset
        /// </summary>
        public Offset Offset { get; }
    }
    
    
    
    /// <summary>
    ///     Calculate a partition number given a <paramref name="partitionCount" />
    ///     and <paramref name="message" />. The <paramref name="topic" />
    ///     is also provided, but is typically not used.
    /// </summary>
    /// <remarks>
    ///     A partitioner instance may be called in any thread at any time and
    ///     may be called multiple times for the same message/key.
    /// 
    ///     A partitioner:
    ///     - MUST NOT block or execute for prolonged periods of time.
    ///     - MUST return a value between 0 and partitionCount-1.
    ///     - MUST NOT throw any exception.
    /// </remarks>
    /// <param name="topic">The topic.</param>
    /// <param name="partitionCount">
    ///     The number of partitions in <paramref name="topic" />.
    /// </param>
    /// <param name="message">The message to select partition for.</param>
    /// <returns>
    ///     The calculated <seealso cref="T:Confluent.Kafka.Partition" />, possibly
    ///     <seealso cref="F:Confluent.Kafka.Partition.Any" />.
    /// </returns>
    public delegate Partition QuixPartitionerDelegate(string topic, int partitionCount, KafkaMessage message);
}