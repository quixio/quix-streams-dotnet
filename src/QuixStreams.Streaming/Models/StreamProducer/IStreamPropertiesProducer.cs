using System;
using System.Collections.ObjectModel;

namespace QuixStreams.Streaming.Models.StreamProducer
{
    /// <summary>
    /// Represents properties and metadata of the stream.
    /// All changes to these properties are automatically published to the underlying stream.
    /// </summary>
    public interface IStreamPropertiesProducer : IDisposable
    {
        /// <summary>
        /// Name of the stream.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Specify location of the stream in data catalogue.
        /// For example: /cars/ai/carA/.
        /// </summary>
        string Location { get; set; }

        /// <summary>
        /// Date Time of stream recording. Commonly set to Datetime.UtcNow.
        /// </summary>
        DateTime? TimeOfRecording { get; set; }

        /// <summary>
        /// Automatic flush interval of the properties metadata into the channel [ in milliseconds ]
        /// </summary>
        int FlushInterval { get; set; }

        /// <summary>
        /// Metadata of the stream.
        /// </summary>
        ObservableDictionary<string, string> Metadata { get; }

        /// <summary>
        /// List of Stream Ids of the Parent streams
        /// </summary>
        ObservableCollection<string> Parents { get; }

        /// <summary>
        /// Adds a parent stream.
        /// </summary>
        /// <param name="parentStreamId">Stream Id of the parent</param>
        void AddParent(string parentStreamId);

        /// <summary>
        /// Removes a parent stream
        /// </summary>
        /// <param name="parentStreamId">Stream Id of the parent</param>
        void RemoveParent(string parentStreamId);

        /// <summary>
        /// Immediately writes the properties yet to be sent instead of waiting for the flush timer (20ms)
        /// </summary>
        void Flush();
    }
}