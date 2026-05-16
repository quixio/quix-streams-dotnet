using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuixStreams.Kafka.Transport.SerDes.Codecs.JsonConverters
{
    /// <summary>
    /// Converts string-backed model identifiers as JSON strings.
    /// </summary>
    public sealed class CodecIdJsonConverter : JsonConverter<CodecId>
    {
        public override CodecId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, CodecId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// Converts string-backed model keys as JSON strings.
    /// </summary>
    public sealed class ModelKeyJsonConverter : JsonConverter<ModelKey>
    {
        public override ModelKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, ModelKey value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
