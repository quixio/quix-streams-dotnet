using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka.Transport.SerDes.Legacy;

namespace QuixStreams.Kafka.Transport.SerDes
{
    /// <summary>
    /// Splits Kafka messages into multiple message
    /// </summary>
    public interface IKafkaMessageSplitter
    {
        /// <summary>
        /// Splits the kafka message
        /// </summary>
        /// <param name="message">The kafka message to split</param>
        /// <returns>The transport packages resulting from the split</returns>
        IEnumerable<KafkaMessage> Split(KafkaMessage message);

        /// <summary>
        /// Returns whether the message should be split according to implementation logic
        /// </summary>
        /// <param name="message">The message to check if needs splitting</param>
        /// <returns>Whether splitting is required</returns>
        bool ShouldSplit(KafkaMessage message);
    }

    /// <summary>
    /// Splits Kafka messages into multiple message
    /// </summary>
    public class KafkaMessageSplitter : IKafkaMessageSplitter
    {
        /// <summary>
        /// The maximum size of a kafka message including header, key, value 
        /// </summary>
        internal readonly int MaximumKafkaMessageSize;
        
        /// <summary>
        /// The amount of segments at which a warning message should be logged
        /// </summary>
        private const int VerboseWarnAboveSegmentCount = 3;
        
        /// <summary>
        /// The message size above which a warning message should be logged. This is a moving size to avoid
        /// constant spamming of warning
        /// </summary>
        private static int MovingWarnAboveSize = 0;
        
        private static readonly ILogger logger = Logging.CreateLogger(typeof(KafkaMessageSplitter));

        /// <summary>
        /// The expected size of the the details to describe the split info
        /// </summary>
        internal static int ExpectedHeaderSplitInfoSize = KafkaMessage.EstimateHeaderSize(
            CreateSegmentDictionary(int.MaxValue, BitConverter.GetBytes(int.MaxValue), Guid.NewGuid().ToByteArray()));

        internal static int ExpectedCompressionInfoSize = KafkaMessage.EstimateHeaderSize(new Dictionary<string, byte[]>()
        {
            { Constants.KafkaMessageHeaderCodecId, new KafkaHeader("", Constants.KafkaMessageHeaderCodecIdGZipCompression).Value}
        });
        
        /// <summary>
        /// Initializes a new instance of <see cref="KafkaMessageSplitter"/>
        /// </summary>
        /// <param name="maximumKafkaMessageSize">The maximum message size kafka accepts</param>
        public KafkaMessageSplitter(int maximumKafkaMessageSize)
        {
            this.MaximumKafkaMessageSize = maximumKafkaMessageSize;
        }
        
        /// <summary>
        /// Splits the kafka message
        /// </summary>
        /// <param name="message">The kafka message to split</param>
        /// <returns>The transport packages resulting from the split</returns>
        public IEnumerable<KafkaMessage> Split(KafkaMessage message)
        {
            if (message.MessageSize <= this.MaximumKafkaMessageSize)
            {
                yield return message;
                yield break;
            }

            var segmentCount = 0;
            if (PackageSerializationSettings.Mode == PackageSerializationMode.LegacyValue)
            {
                foreach (var splitMessage in LegacySplit(message))
                {
                    segmentCount++;
                    yield return splitMessage;
                }
                WarningCheck(segmentCount, message.Value.Length);
                yield break;
            }
            
            foreach (var splitMessage in HeaderSplit(message))
            {
                segmentCount++;
                yield return splitMessage;
            }
            WarningCheck(segmentCount, message.Value.Length);
        }

        /// <inheritdoc/>
        public bool ShouldSplit(KafkaMessage message)
        {
            if (message == null) return false;
            return message.MessageSize > this.MaximumKafkaMessageSize;
        }


        private IEnumerable<KafkaMessage> HeaderSplit(KafkaMessage message)
        {
            var valueSizeMax = this.MaximumKafkaMessageSize - message.HeaderSize - (message.Key?.Length ?? 0);
            valueSizeMax -= ExpectedHeaderSplitInfoSize;

            if (valueSizeMax < 1) // The message can't realistically be split, so just return it and log a warning
            {
                logger.LogWarning("A message could not be split because your message limit is {0} bytes.", this.MaximumKafkaMessageSize);
                yield return message;
            }
            
            var compressedMessage = CompressMessage(message);
            KafkaMessage messageToSplit = compressedMessage;
            if (compressedMessage.MessageSize > message.MessageSize)
            {
                // Use original, less overhead
                messageToSplit = message;
                compressedMessage = null;
            }
            else
            {
                var compressedValueSizeMax = this.MaximumKafkaMessageSize - compressedMessage.HeaderSize - (compressedMessage.Key?.Length ?? 0);
                var compressedCount = (int)Math.Ceiling((double)compressedMessage.Value.Length / compressedValueSizeMax);
                if (compressedCount != 1)
                {
                    // In this case we have to recalculate count assuming we have to add split info
                    compressedValueSizeMax -= ExpectedHeaderSplitInfoSize;
                    if (compressedValueSizeMax < -1) compressedCount = int.MaxValue;
                    else compressedCount = (int)Math.Ceiling((double)compressedMessage.Value.Length / compressedValueSizeMax);
                }
                var nonCompressedCount = (int)Math.Ceiling((double)message.Value.Length / valueSizeMax);

                if (nonCompressedCount <= compressedCount)
                {
                    // Use original, less overhead
                    messageToSplit = message;
                    compressedMessage = null;
                }
                else
                {
                    valueSizeMax = compressedValueSizeMax;
                    if (compressedCount == 1) 
                    {
                        // Due to compression there is is nothing to split
                        yield return messageToSplit;
                        yield break;
                    }
                }
            }

            var messageId = Guid.NewGuid().ToByteArray();
            var count = (int)Math.Ceiling((double)messageToSplit.Value.Length / valueSizeMax);
            var countAsBytes = BitConverter.GetBytes(count);
            var start = 0;
            for (int index = 0; index < count; index++)
            {
                var end = Math.Min(start + valueSizeMax, messageToSplit.Value.Length); // non inclusive
                var length = end - start;
                var segment = new byte[length];
                Array.Copy(messageToSplit.Value, start, segment, 0, length);
                var headers = CreateSegmentDictionary(index, countAsBytes, messageId);
                var messageHeaders = messageToSplit.Headers?.ToList() ?? new List<KafkaHeader>();
                foreach (var header in headers)
                {
                    messageHeaders.Add(new KafkaHeader(header.Key, header.Value));
                }

                var segmentMessage = new KafkaMessage(messageToSplit.Key, segment, messageHeaders.ToArray());
                yield return segmentMessage;
                start = end;
            }
        }

        private KafkaMessage CompressMessage(KafkaMessage message)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
            {
                zipStream.Write(message.Value, 0, message.Value.Length);
                zipStream.Close();
                var headers = ExtendHeaderByN(message.Headers, 0);
                
                var updated = false;
                for(var index = 0; index < headers.Length; index++)
                {
                    var header = headers[index];
                    if (header.Key != Constants.KafkaMessageHeaderCodecId) continue;
                    var value = Encoding.UTF8.GetString(header.Value);
                    var newValue = $"{Constants.KafkaMessageHeaderCodecIdGZipCompression}{value}";
                    updated = true;
                    headers[index] = new KafkaHeader(header.Key, newValue);
                }

                if (!updated)
                {
                    headers = ExtendHeaderByN(headers, 1);
                    headers[headers.Length-1] = new KafkaHeader(Constants.KafkaMessageHeaderCodecId, Constants.KafkaMessageHeaderCodecIdGZipCompression);
                }

                var compressed = compressedStream.ToArray();
                
                return new KafkaMessage(message.Key, compressed, headers, message.Timestamp);
            }
        }

        private KafkaHeader[] ExtendHeaderByN(KafkaHeader[] headers, int number)
        {
            if (headers == null) return new KafkaHeader[number];
            var extendedHeaders = new KafkaHeader[headers.Length + number];
            for (var index = 0; index < headers.Length; index++)
            {
                var kafkaHeader = headers[index];
                extendedHeaders[index] = kafkaHeader;
            }

            return extendedHeaders;
        }

        internal static IDictionary<string, byte[]> CreateSegmentDictionary(int index, byte[] countBytes, byte[] messageGuidBytes)
        {
            return new Dictionary<string, byte[]>
            {
                { Constants.KafkaMessageHeaderSplitMessageId, messageGuidBytes },
                { Constants.KafkaMessageHeaderSplitMessageCount, countBytes },
                { Constants.KafkaMessageHeaderSplitMessageIndex, BitConverter.GetBytes(index) }
            };
        }

        private IEnumerable<KafkaMessage> LegacySplit(KafkaMessage message)
        {
            var valueSizeMax = this.MaximumKafkaMessageSize - message.HeaderSize - (message.Key?.Length ?? 0);
            
            foreach (var segment in LegacyByteSplitter.Split(message.Value, valueSizeMax))
            {
                yield return new KafkaMessage(message.Key, segment, message.Headers);
            }
        }

        private void WarningCheck(int segmentCount, int messageLength)
        {
            if (segmentCount <= VerboseWarnAboveSegmentCount) return;
                
            if (messageLength > MovingWarnAboveSize)
            {
                // not thread safe, but better than having too many warnings or some performance implication 
                MovingWarnAboveSize = Math.Max(messageLength, MovingWarnAboveSize) * 2;
                logger.LogWarning("One or more of your messages exceed the optimal size. Consider publishing smaller for better consumer experience. Your message was over {0}KB.", Math.Round((double)messageLength/1024, 1));
            }
            else
            {
                logger.LogTrace("One or more of your messages exceed the optimal size. Consider publishing smaller for better consumer experience. Your message was over {0}KB.", Math.Round((double)messageLength/1024, 1));
            }
        }
    }
}