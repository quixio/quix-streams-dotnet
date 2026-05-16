using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using QuixStreams.Kafka.Transport.SerDes.Codecs;
using QuixStreams.Kafka.Transport.SerDes.Codecs.DefaultCodecs;

namespace QuixStreams.Telemetry.Models.Telemetry.Parameters.Codecs
{
    /// <summary>
    /// TimeseriesData Json Codec implementation
    /// </summary>
    public class TimeseriesDataCustomJsonCodec : Codec<TimeseriesDataRaw>
    {
        private static readonly DefaultJsonCodec<TimeseriesDataRaw> BaseCodec = new DefaultJsonCodec<TimeseriesDataRaw>();
        /// <inheritdoc />
        public override CodecId Id => BaseCodec.Id; // this is only a serialization codec, still valid JSON

        private Converter JsonConverter = new Converter();

        /// <inheritdoc />
        public override TimeseriesDataRaw Deserialize(byte[] contentBytes)
        {
            using (var document = JsonDocument.Parse(contentBytes))
            {
                return JsonConverter.ReadJson(document.RootElement);
            }
        }

        /// <inheritdoc />
        public override TimeseriesDataRaw Deserialize(ArraySegment<byte> contentBytes)
        {
            using (var document = JsonDocument.Parse(contentBytes))
            {
                return JsonConverter.ReadJson(document.RootElement);
            }
        }



        /// <inheritdoc />
        public override byte[] Serialize(TimeseriesDataRaw obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var jsonWriter = new Utf8JsonWriter(memoryStream))
                {
                    JsonConverter.WriteJson(jsonWriter, obj);
                }

                return memoryStream.ToArray();
            }
        }

        public class Converter
        {
            public TimeseriesDataRaw ReadJson(JsonElement root)
            {
                long epoch = 0;
                var size = -1;
                long[] timestamps = null;
                Dictionary<string, double?[]> numericValues = null;
                Dictionary<string, string[]> stringValues = null;
                Dictionary<string, byte[][]> binaryValues = null;
                Dictionary<string, string[]> tagValues = null;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"Expected object, found {root.ValueKind}");
                }

                foreach (var property in root.EnumerateObject())
                {
                    switch (property.Name)
                    {
                        case "Epoch":
                            epoch = property.Value.GetInt64();
                            break;
                        case "Timestamps":
                            timestamps = ParseArray(property.Value, ref size, o => o.GetInt64());
                            break;
                        case "NumericValues":
                            numericValues = ParseDict(property.Value, ref size, o =>
                                o.ValueKind == JsonValueKind.Null ? (double?)null : o.GetDouble());
                            break;
                        case "StringValues":
                            stringValues = ParseDict(property.Value, ref size, o =>
                                o.ValueKind == JsonValueKind.Null ? null : o.GetString());
                            break;
                        case "BinaryValues":
                            binaryValues = ParseDict(property.Value, ref size, o =>
                                o.ValueKind == JsonValueKind.Null ? null : o.GetBytesFromBase64());
                            break;
                        case "TagValues":
                            tagValues = ParseDict(property.Value, ref size, o =>
                                o.ValueKind == JsonValueKind.Null ? null : o.GetString());
                            break;
                    }
                }


                return new TimeseriesDataRaw
                {
                    Epoch = epoch,
                    Timestamps = timestamps?.ToArray() ?? Array.Empty<long>(),
                    NumericValues = numericValues ?? new Dictionary<string, double?[]>(),
                    StringValues = stringValues ?? new Dictionary<string, string[]>(),
                    BinaryValues = binaryValues ?? new Dictionary<string, byte[][]>(),
                    TagValues = tagValues ?? new Dictionary<string, string[]>()
                };
            }

            private Dictionary<string, T[]> ParseDict<T>(JsonElement element, ref int size, Func<JsonElement, T> converter)
            {
                var dict = new Dictionary<string, T[]>();
                if (element.ValueKind != JsonValueKind.Object)
                    throw new JsonException("TagValues serialization error");
                foreach (var property in element.EnumerateObject())
                {
                    var values = ParseArray(property.Value, ref size, converter);
                    dict.Add(property.Name, values);
                }

                return dict;
            }

            private T[] ParseArray<T>(JsonElement element, ref int size, Func<JsonElement, T> converter)
            {
                if (element.ValueKind != JsonValueKind.Array)
                    throw new JsonException($"Expected StartArray, found {element.ValueKind}");

                if (size == -1)
                {
                    var result = new List<T>();
                    foreach (var item in element.EnumerateArray())
                    {
                        result.Add(converter(item));
                    }

                    size = result.Count;

                    return result.ToArray();
                }

                var resultArr = new T[size];
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    resultArr[index++] = converter(item);
                return resultArr;
            }

            public void WriteJson(Utf8JsonWriter writer, TimeseriesDataRaw value)
            {
                writer.WriteStartObject();
                writer.WriteNumber(nameof(value.Epoch), value.Epoch);

                writer.WritePropertyName(nameof(value.Timestamps));
                SerializeArray(writer, value.Timestamps);

                if (value.NumericValues?.Count > 0)
                {
                    writer.WritePropertyName(nameof(value.NumericValues));
                    SerializeDictionary(writer, value.NumericValues);
                }

                if (value.StringValues?.Count > 0)
                {
                    writer.WritePropertyName(nameof(value.StringValues));
                    SerializeDictionary(writer, value.StringValues);
                }

                if (value.BinaryValues?.Count > 0)
                {
                    writer.WritePropertyName(nameof(value.BinaryValues));
                    SerializeDictionary(writer, value.BinaryValues);
                }

                if (value.TagValues?.Count > 0)
                {
                    writer.WritePropertyName(nameof(value.TagValues));
                    SerializeDictionary(writer, value.TagValues);
                }

                writer.WriteEndObject();

            }

            private void SerializeDictionary<T>(Utf8JsonWriter writer, Dictionary<string, T[]> dict)
            {
                writer.WriteStartObject();

                foreach (var kvp in dict)
                {
                    writer.WritePropertyName(kvp.Key);
                    SerializeArray(writer, kvp.Value);
                }

                writer.WriteEndObject();
            }

            private void SerializeArray<T>(Utf8JsonWriter writer, T[] array)
            {
                writer.WriteStartArray();

                for (int i = 0; i < array.Length; i++)
                {
                    var val = array[i];
                    if (val != null) JsonSerializer.Serialize(writer, val);
                    else writer.WriteNullValue();
                }

                writer.WriteEndArray();
            }
        }
    }
}
