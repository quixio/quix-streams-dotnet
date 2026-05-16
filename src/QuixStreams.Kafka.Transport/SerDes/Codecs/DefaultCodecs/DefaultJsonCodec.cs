using System;
using System.Text.Json;
using QuixStreams.Kafka.Transport.SerDes.Json;

namespace QuixStreams.Kafka.Transport.SerDes.Codecs.DefaultCodecs
{
    /// <summary>
    /// Default JSON codec for any type.
    /// </summary>
    /// <typeparam name="TContent"></typeparam>
    public class DefaultJsonCodec<TContent> : Codec<TContent>
    {
        /// <inheritdoc />
        public override CodecId Id => CodecId.WellKnownCodecIds.DefaultTypedJsonCodec;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultJsonCodec{TContent}"/>
        /// </summary>
        public DefaultJsonCodec()
        {
        }


        /// <inheritdoc />
        public override TContent Deserialize(byte[] contentBytes)
        {
            return JsonSerializer.Deserialize<TContent>(contentBytes, QuixJsonOptions.Default);
        }
        
        /// <inheritdoc />
        public override TContent Deserialize(ArraySegment<byte> contentBytes)
        {
            return JsonSerializer.Deserialize<TContent>(contentBytes, QuixJsonOptions.Default);
        }

        /// <inheritdoc />
        public override byte[] Serialize(TContent obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj, QuixJsonOptions.Default);
        }
    }

    /// <summary>
    /// Default Json codec
    /// </summary>
    public class DefaultJsonCodec : ICodec
    {
        /// <summary>
        /// Static instance of Default Json codec
        /// </summary>
        public static readonly DefaultJsonCodec Instance = new DefaultJsonCodec();
        
        private DefaultJsonCodec()
        {
        }

        /// <inheritdoc />
        public CodecId Id => CodecId.WellKnownCodecIds.DefaultJsonCodec;

        /// <inheritdoc />
        public bool TrySerialize(object obj, out byte[] serialized)
        {
            serialized = null;
            try
            {
                serialized = JsonSerializer.SerializeToUtf8Bytes(obj, QuixJsonOptions.Default);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize(byte[] contentBytes, out object content)
        {
            content = null;
            try
            {
                content = JsonSerializer.Deserialize<object>(contentBytes, QuixJsonOptions.Default);
                return content != null;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserialize(ArraySegment<byte> contentBytes, out object content)
        {
            content = null;
            try
            {
                content = JsonSerializer.Deserialize<object>(contentBytes, QuixJsonOptions.Default);
                return content != null;
            }
            catch
            {
                return false;
            }        
        }

        /// <inheritdoc/>
        public Type Type => typeof(object);
    }
}
