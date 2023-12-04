using Confluent.Kafka;
using QuixStreams.Kafka;
using QuixStreams.Streaming.Utils;
using QuixStreams.Telemetry.Kafka;
using QuixStreams.Telemetry.Models;
using AutoOffsetReset = QuixStreams.Telemetry.Kafka.AutoOffsetReset;

namespace QuixStreams.Streaming.TestHelpers;

/// <summary>
/// File streaming client intended for testing, rather than any real world use case
/// </summary>
public class FileStreamingClient
{
    private readonly string basePath;
    private readonly object createLock = new object();
    private Dictionary<string, ITopicConsumer> consumers = new Dictionary<string, ITopicConsumer>();
    private Dictionary<string, ITopicProducer> producers = new Dictionary<string, ITopicProducer>();

    public FileStreamingClient(string basePath = null, bool createIfDoesNotExist = false)
    {
        var _ = CodecSettings.CurrentCodec; //Causes init, maybe there is a better way
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "file_topics");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

        }

        if (!Directory.Exists(basePath) && !createIfDoesNotExist)
        {
            throw new Exception($"Path '{{basePath}}' does not exist.");
        }

        this.basePath = basePath;
    }
    
    public ITopicConsumer GetFileConsumer(string fileName, AutoOffsetReset autoOffset = AutoOffsetReset.Latest)
    {
        var path = GetPath(fileName);
        
        if (this.consumers.TryGetValue(path, out var existing)) return existing;
        lock (this.createLock)
        {
            if (this.consumers.TryGetValue(path, out existing)) return existing;
            var fileConsumer = new FileTopicConsumer(path, autoOffset);
            var consumer = new TopicConsumer(new TelemetryKafkaConsumer(fileConsumer, null));
            this.consumers[path] = consumer;
            return consumer;
        }
    }

    public ITopicProducer GetFileProducer(string fileName)
    {
        var path = GetPath(fileName);
        if (this.producers.TryGetValue(path, out var existing)) return existing;
        lock (this.createLock)
        {
            if (this.producers.TryGetValue(path, out existing)) return existing;
            var fileProducer = new FileTopicProducer(path);
            TelemetryKafkaProducer CreateKafkaProducer(string streamId)
            {
                return new TelemetryKafkaProducer(fileProducer, streamId);
            }

            var producer = new TopicProducer(CreateKafkaProducer);
            this.producers[path] = producer;
            return producer;
        }
    }

    private string GetPath(string fileName)
    {
        if (Path.IsPathRooted(fileName)) return fileName;
        return Path.GetFullPath(Path.Combine(this.basePath, fileName));
    }
}

public class FileTopicProducer : IKafkaProducer
{
    private FileStream fileStream = null;
    private object writeLock = new object();
    
    public FileTopicProducer(string path)
    {
        fileStream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
    }

    public void Dispose()
    {
        this.Flush(default);
        fileStream?.Dispose();
    }

    public Task Publish(KafkaMessage message, CancellationToken cancellationToken = default)
    {
        lock (writeLock)
        {
            fileStream.Write(FileSerDes.Serialize(message));
            fileStream.Flush();
        }

        return Task.CompletedTask;
    }

    public Task Publish(IEnumerable<KafkaMessage> messages, CancellationToken cancellationToken = default)
    {
        lock (writeLock)
        {
            foreach (var message in messages)
            {
                fileStream.Write(FileSerDes.Serialize(message));   
            }
            fileStream.Flush();
        }

        return Task.CompletedTask;
    }

    public void Flush(CancellationToken cancellationToken)
    {
        lock (writeLock)
        {
            // If acquired lock successfully, it means there is nothing else writing
        }
    }

    public int MaxMessageSizeBytes { get; } = 1000000; // Kafka default
}

public class FileTopicConsumer : IKafkaConsumer
{
    private FileStream fileStream = null;
    private readonly string path;
    private readonly AutoOffsetReset autoOffset;
    private bool opened = false;
    private bool closing = false;
    private object openLock = new object();
    private Task worker;
    private readonly string topicName;

    public FileTopicConsumer(string path, AutoOffsetReset autoOffset)
    {
        this.path = path;
        this.autoOffset = autoOffset;
        this.topicName = Path.GetFileName(path);
    }

    public void Dispose()
    {
        this.fileStream.Dispose();
    }

    public Func<KafkaMessage, Task> OnMessageReceived { get; set; } = message => Task.CompletedTask;
    public event EventHandler<Exception>? OnErrorOccurred;
    public void Commit(ICollection<TopicPartitionOffset> partitionOffsets)
    {
        // Do nothing for now
    }

    public void Commit()
    {
        // Do nothing for now
    }

    public event EventHandler<CommittedEventArgs>? OnCommitted;
    public event EventHandler<CommittingEventArgs>? OnCommitting;
    public event EventHandler<RevokingEventArgs>? OnRevoking;
    public event EventHandler<RevokedEventArgs>? OnRevoked;
    
    
    public void Open()
    {
        if (this.opened) return;
        lock (openLock)
        {
            if (this.opened) return;
            if (!Path.Exists(path))
            {
                using (File.Create(path))
                {
                    // Don't want write permission
                }
            }

            fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (autoOffset == AutoOffsetReset.Latest)
            {
                fileStream.Seek(0, SeekOrigin.End);
            }

            this.worker = Task.Run(this.Worker);
            this.opened = true;
        }
    }

    public void Close()
    {
        if (!this.opened) return;
        lock (openLock)
        {
            if (!this.opened) return;
            this.closing = true;
            this.worker.GetAwaiter().GetResult();
            try
            {
                this.fileStream.Dispose();
            } catch (Exception) {}
            this.opened = false;
            this.closing = false;
        }
    }

    private async Task Worker()
    {
        using var br = new BinaryReader(this.fileStream);
        while (!this.closing)
        {
            var msg = FileSerDes.GetNextKafkaMessage(br, this.topicName);
            if (msg == null)
            {
                // Yeah, surely it can be done better but for a test class good enough
                await Task.Delay(500);
                continue;
            }

            try
            {
                await this.OnMessageReceived.Invoke(msg);
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke(this, ex);
            }
        }
    }
}

public class FileSerDes
{
    public static Span<Byte> Serialize(KafkaMessage kafkaMessage)
    {
        var stream = new MemoryStream(kafkaMessage.MessageSize);
        using var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(int.MaxValue); // irrelevant, will come back to update
        binaryWriter.Flush();
        var start = stream.Position;
        
        binaryWriter.Write(kafkaMessage.Timestamp.UnixTimestampMs);
        binaryWriter.Write((byte)kafkaMessage.Timestamp.Type);

        binaryWriter.Write(kafkaMessage.Key?.Length ?? 0);
        if ((kafkaMessage.Key?.Length ?? 0) > 0) binaryWriter.Write(kafkaMessage.Key!);
        binaryWriter.Write(kafkaMessage.Value.Length);
        binaryWriter.Write(kafkaMessage.Value);
        binaryWriter.Write(kafkaMessage.Headers?.Length ?? 0);
        if ((kafkaMessage.Headers?.Length ?? 0) > 0)
        {
            foreach (var header in kafkaMessage.Headers!)
            {
                binaryWriter.Write(header.Key);
                binaryWriter.Write(header.Value.Length);
                binaryWriter.Write(header.Value);
            }
        }

        if (kafkaMessage.TopicPartitionOffset != null)
        {
            var offset = kafkaMessage.TopicPartitionOffset;
            binaryWriter.Write(offset.Partition.Value);
            binaryWriter.Write(offset.Offset.Value);
            // not writing the topic itself, as this whole thing is supposed to be for a single topic
        }
        binaryWriter.Flush();
        var end = (int)stream.Position;
        stream.Seek(0, SeekOrigin.Begin);
        binaryWriter.Write(end-start);
        binaryWriter.Flush();
        return new Span<byte>(stream.ToArray(), 0, end);
    }

    private static KafkaMessage Deserialize(byte[] kafkaMessageBytes, string topicName, long streamOffset)
    {
        var memoryStream = new MemoryStream(kafkaMessageBytes);
        using var sr = new BinaryReader(memoryStream);
        var timestamp = new Timestamp(sr.ReadInt64(), (TimestampType)sr.ReadByte());
        var keyLength = sr.ReadInt32();
        byte[] key = null;
        if (keyLength > 0) key = sr.ReadBytes(keyLength);
        var valueLength = sr.ReadInt32();
        byte[] value = sr.ReadBytes(valueLength);
        var headerCount = sr.ReadInt32();
        KafkaHeader[] headers = new KafkaHeader[headerCount];
        for (var index = 0; index < headerCount; index++)
        {
            var headerKey = sr.ReadString();
            var headerValLength = sr.ReadInt32();
            var headerVal = sr.ReadBytes(headerValLength);
            headers[index] = new KafkaHeader(headerKey, headerVal);
        }

        TopicPartitionOffset? topicPartitionOffset = null;

        if (memoryStream.Position != memoryStream.Length)
        {
            var partition = sr.ReadInt32();
            var offset = sr.ReadInt64();
            topicPartitionOffset = new TopicPartitionOffset(topicName, partition, offset);
        }
        else
        {
            topicPartitionOffset = new TopicPartitionOffset(topicName, 0, streamOffset);
        }

        return new KafkaMessage(key, value, headers, timestamp, topicPartitionOffset);

    }

    public static KafkaMessage? GetNextKafkaMessage(BinaryReader reader, string topicName)
    {
        if (reader.BaseStream.Position == reader.BaseStream.Length)
        {
            return null;
        }
        var len = reader.ReadInt32();
        return Deserialize(reader.ReadBytes(len), topicName, reader.BaseStream.Position);
    }
}