using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuixStreams.Streaming.QuixApi.Portal;

namespace QuixStreams.Streaming.QuixApi
{
    internal class WorkspaceBrokerTypeJsonConverter : JsonConverter<WorkspaceBrokerType>
    {
        public override void Write(Utf8JsonWriter writer, WorkspaceBrokerType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override WorkspaceBrokerType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                if (Enum.TryParse(value, out WorkspaceBrokerType result))
                {
                    return result;
                }
            }

            return WorkspaceBrokerType.Unknown;
        }
    }
}
