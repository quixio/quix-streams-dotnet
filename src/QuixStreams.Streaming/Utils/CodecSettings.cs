using System;
using Microsoft.Extensions.Logging;
using QuixStreams.Kafka.Transport.SerDes;
using QuixStreams.Kafka.Transport.SerDes.Legacy.MessageValue;
using QuixStreams.Telemetry.Models;

namespace QuixStreams.Streaming.Utils
{
    /// <summary>
    /// Global Codec settings for streams.
    /// </summary>
    public static class CodecSettings
    {
        /// <summary>
        /// The currently configured codec
        /// </summary>
        public static CodecType CurrentCodec;

        /// <summary>
        /// Used to avoid marking CurrentCodec nullable or adding unused enum to it
        /// </summary>
        private static bool codecSet = false;

        /// <summary>
        /// The logger for the class
        /// </summary>
        private static Lazy<ILogger> logger = new Lazy<ILogger>(() => Logging.CreateLogger(typeof(CodecSettings)));

        static CodecSettings()
        {
            // Set the Json codec type as the default
            SetGlobalCodecType(CodecType.Json);
        }
        
        /// <summary>
        /// Sets the codec type to be used by producers and transfer package value serialization 
        /// </summary>
        /// <param name="codecType"></param>
        public static void SetGlobalCodecType(CodecType codecType)
        {
            if (CurrentCodec == codecType && codecSet) return;
            CodecRegistry.Register(producerCodec: codecType);
            
            if (codecType == CodecType.Protobuf)
            {
                PackageSerializationSettings.LegacyValueCodecType = TransportPackageValueCodecType.Binary;
            }
            else
            {
                PackageSerializationSettings.LegacyValueCodecType = TransportPackageValueCodecType.Json;
            }
            CurrentCodec = codecType;
            codecSet = true;
            logger.Value.LogDebug("Codecs are configured to publish using {0} with {1} package codec.", codecType, PackageSerializationSettings.LegacyValueCodecType);
        }
    }
}