using System;
using System.Collections.Generic;

namespace QuixStreams.Streaming.Models.StreamConsumer
{
    /// <summary>
    /// Represents properties and metadata of the stream.
    /// All changes to these properties are automatically populated to this class.
    /// </summary>
    public interface IStreamPropertiesConsumer : IDisposable
    {
        /// <summary>
        /// Raised when the stream properties change
        /// </summary>
        event EventHandler<StreamPropertiesChangedEventArgs> OnChanged;

        /// <summary>
        /// Gets the name of the stream
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the location of the stream
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Gets the datetime of the recording
        /// </summary>
        DateTime? TimeOfRecording { get; }

        /// <summary>
        /// Gets the metadata of the stream
        /// </summary>
        Dictionary<string, string> Metadata { get; }

        /// <summary>
        /// Gets the list of Stream IDs for the parent streams
        /// </summary>
        List<string> Parents { get; }
    }
}