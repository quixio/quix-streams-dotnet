using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using QuixStreams;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Streaming.Models.StreamProducer
{
    /// <summary>
    /// Represents properties and metadata of the stream.
    /// All changes to these properties are automatically published to the underlying stream.
    /// </summary>
    public class StreamPropertiesProducer : IStreamPropertiesProducer
    {
        private readonly IStreamProducerInternal streamProducer;
        private string name;
        private string location;
        private DateTime? timeOfRecording;

        private readonly Timer flushTimer;
        private bool timerEnabled = false; // Here because every now and then resetting its due time to never doesn't work
        private const int PropertyChangedFlushInterval = 20;
        private int lastHash = 0;
        private readonly object flushLock = new object();
        private bool isDisposed = false;

        private long lastHeartbeatRebroadcastTime = 0;  // in milliseconds
        private int heartbeatRebroadcastFlushInterval = 30*1000;
        private readonly ILogger<StreamTimeseriesProducer> logger = QuixStreams.Logging.CreateLogger<StreamTimeseriesProducer>();

        /// <inheritdoc/>
        public int FlushInterval
        {
            get
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
                }
                return this.heartbeatRebroadcastFlushInterval;
            }
            set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
                }

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),"Flush interval must be greater than 0");
                }
                this.heartbeatRebroadcastFlushInterval = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StreamPropertiesProducer"/>
        /// </summary>
        /// <param name="streamProducer">Stream writer owner</param>
        internal StreamPropertiesProducer(IStreamProducerInternal streamProducer)
        {
            this.streamProducer = streamProducer;
            streamProducer.OnBeforeSend += new Action<Type>((type =>
            {
                // To handle ClosePacket
                if (isDisposed)
                    return;

                // To avoid circular infinite recursion
                if (type != typeof(StreamProperties))
                {
                    this.CheckForHeartbeatFlush();
                }
            }));

            this.Metadata = new ObservableDictionary<string, string>();
            this.Metadata.CollectionChanged += (sender, e) =>
            {
                this.PushWrite();
            };
            this.Parents = new ObservableCollection<string>();
            this.Parents.CollectionChanged += (sender, e) =>
            {
                this.PushWrite();
            };

            // Timer for delayed writes
            flushTimer = new Timer((state) =>
            {
                if (!timerEnabled) return;
                this.Flush();
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <inheritdoc/>
        public string Name
        {
            get => name; set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
                }
                name = value;
                this.PushWrite();
            }
        }

        /// <inheritdoc/>
        public string Location
        {
            get => location; set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
                }
                location = value;
                this.PushWrite();
            }
        }

        /// <inheritdoc/>
        public DateTime? TimeOfRecording
        {
            get => timeOfRecording; set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
                }
                timeOfRecording = value;
                this.PushWrite();
            }
        }
        
        private void CheckForHeartbeatFlush()
        {
            long curms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (curms > this.lastHeartbeatRebroadcastTime + this.FlushInterval)
            {
                this.Flush(false, false);
            }
        }

        /// <inheritdoc/>
        public ObservableDictionary<string, string> Metadata { get; }

        /// <inheritdoc/>
        public ObservableCollection<string> Parents { get; }

        /// <inheritdoc/>
        public void AddParent(string parentStreamId)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
            }
            // TODO REMOVE this or add extra logic to validate it
            this.Parents.Add(parentStreamId);
        }

        /// <inheritdoc/>
        public void RemoveParent(string parentStreamId)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
            }
            // TODO REMOVE this or add extra logic to validate it
            this.Parents.Remove(parentStreamId);
        }

        /// <inheritdoc/>
        public void Flush()
        {
            this.Flush(false);
        }

        private void Flush(bool force, bool flushOnlyOnChange = true)
        {
            if (!force && isDisposed)
            {
                throw new ObjectDisposedException(nameof(StreamPropertiesProducer));
            }


            timerEnabled = false;
            this.flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
            lock (flushLock)
            {
                
                var streamProperties = new StreamProperties
                {
                    Location = this.location,
                    Name = this.name,
                    Parents = GetParents(),
                    Metadata = GetMetadata(),
                    TimeOfRecording = this.timeOfRecording
                };

                var hash = streamProperties.GetHashCode();
                if (flushOnlyOnChange && hash == lastHash)
                {
                    return;
                }

                this.lastHash = hash;
                this.lastHeartbeatRebroadcastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                this.streamProducer.Publish(streamProperties);
            }


            List<string> GetParents()
            {
                // This is the easiest way to avoid collection modification exception. Any other solution would require me to
                // have a synchronizing object within the observable list or reinvent an observable concurrent bag. Due to the number of times
                // this can happen (on every flush) that would end up being more expensive than this brute-force
                while (true)
                {
                    try
                    {
                        return this.Parents.ToList();
                    }
                    catch (System.ArgumentException ex)
                    {
                        this.logger.LogTrace(ex, "Exception while trying to get stream metadata");
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        this.logger.LogTrace(ex, "Exception while trying to get stream parents");
                    }
                }
            }
            
            Dictionary<string, string> GetMetadata()
            {
                // This is the easiest way to avoid collection modification exception. Any other solution would require me to
                // have a synchronizing object within the observable dictionary or reinvent an observable concurrent dictionary. Due to the number of times
                // this can happen (on every flush) that would end up being more expensive than this brute-force
                while (true)
                {
                    try
                    {
                        return this.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value);
                    }
                    catch (System.ArgumentException ex)
                    {
                        this.logger.LogTrace(ex, "Exception while trying to get stream metadata");
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        this.logger.LogTrace(ex, "Exception while trying to get stream metadata");
                    }
                }
            }
        }

        private void PushWrite()
        {
            timerEnabled = true;
            this.flushTimer.Change(PropertyChangedFlushInterval, Timeout.Infinite);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.isDisposed) return;
            this.isDisposed = true;
            this.Flush(true);
            flushTimer?.Dispose();
        }
    }
}
