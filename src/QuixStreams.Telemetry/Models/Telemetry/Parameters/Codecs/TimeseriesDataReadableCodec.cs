using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuixStreams.Kafka.Transport.SerDes.Codecs;
using QuixStreams.Kafka.Transport.SerDes.Codecs.DefaultCodecs;

namespace QuixStreams.Telemetry.Models.Telemetry.Parameters.Codecs
{
    /// <summary>
    /// TimeseriesData Codec implementation that is human readable but not JSON for most purposes
    /// </summary>
    public class TimeseriesDataReadableCodec : Codec<TimeseriesDataRaw>
    {
        private static readonly DefaultJsonCodec<TimeseriesDataCodeDto> BaseCodec = new DefaultJsonCodec<TimeseriesDataCodeDto>();

        /// <inheritdoc />
        public override CodecId Id => BaseCodec.Id + "-PaDa-1";  // Used to be called ParameterData. Keeping it for time being to be backward compatible

        /// <inheritdoc />
        public override TimeseriesDataRaw Deserialize(byte[] contentBytes)
        {
            var doBytes = BaseCodec.Deserialize(contentBytes);
            var ret = ConvertToDo(doBytes);
            return ret;
        }

        /// <inheritdoc />
        public override TimeseriesDataRaw Deserialize(ArraySegment<byte> contentBytes)
        {
            var doBytes = BaseCodec.Deserialize(contentBytes);
            var ret = ConvertToDo(doBytes);
            return ret;
        }

        /// <inheritdoc />
        public override byte[] Serialize(TimeseriesDataRaw obj)
        {
            var dto = ConvertToDto(obj);
            var bytes = BaseCodec.Serialize(dto);
            return bytes;
        }
        
        private TimeseriesDataRaw ConvertToDo(TimeseriesDataCodeDto dto)
        {
            var numerics = dto.Numerics ?? new Dictionary<string, List<object>>();
            var strings = dto.Strings ?? new Dictionary<string, StringValueDto>();
            var binary = dto.Binary ?? new Dictionary<string, byte[][]>();
            var tags = dto.Tags ?? new Dictionary<string, StringValueDto>();
            
            // unpack timestamps to their original value
            var timestamps = new long[dto.Timestamps.Length];
            long last = 0;
            for (var index = 0; index < dto.Timestamps.Length; index++)
            {
                var newDelta = dto.Timestamps[index];
                var newVal = last + newDelta;
                timestamps[index] = newVal;
                last = newVal;
            }
            
            var numericValues = numerics.ToDictionary(x=> x.Key, x=> new double?[timestamps.Length]);
            foreach (var paramPair in numerics)
            {
                double? prevValue = 0;
                var paramNumericValues = numericValues[paramPair.Key];
                int indexToInsertAt = 0;
                bool readingVal = false; // false = reading the number of indices skipped, true = reading value, which is either a single double or a list of doubles
                for (var valIndex = 0; valIndex < paramPair.Value.Count; valIndex++)
                {
                    var val = paramPair.Value[valIndex];
                    if (!readingVal)
                    {
                        indexToInsertAt += GetInt32(val);
                        readingVal = true;
                        continue;
                    }
                    
                    if (IsArray(val))
                    {
                        foreach (var t in EnumerateArray(val))
                        {
                            var value = GetDouble(t);
                            prevValue = value + prevValue; // they're deltas from previous value
                            paramNumericValues[indexToInsertAt] = prevValue;
                            indexToInsertAt++;
                        }

                        readingVal = false;
                        continue;
                    }

                    prevValue = GetDouble(val) + prevValue;
                    paramNumericValues[indexToInsertAt] = prevValue;
                    indexToInsertAt++;
                    readingVal = false;
                }
            }

            Dictionary<string, string[]> GetStringValues(Dictionary<string, StringValueDto> values)
            {
                var doValues = values.ToDictionary(x=> x.Key, x=> new string[timestamps.Length]);
                foreach (var pair in values)
                {
                    var stringValues = doValues[pair.Key];
                    int indexToInsertAt = 0;
                    bool readingVal = false; // false = reading the number of indices skipped, true = reading value, which is either a single double or a list of doubles
                    var uniqueValues = pair.Value.Keys;
                    var indexValues = pair.Value.Values;
                    for (var valIndex = 0; valIndex < indexValues.Count; valIndex++)
                    {
                        var val = indexValues[valIndex];
                        if (!readingVal)
                        {
                            indexToInsertAt += GetInt32(val);
                            readingVal = true;
                            continue;
                        }

                        int valueIndex;
                        if (IsArray(val))
                        {
                            foreach (var t in EnumerateArray(val))
                            {
                                valueIndex = GetInt32(t);
                                stringValues[indexToInsertAt] = uniqueValues[valueIndex];
                                indexToInsertAt++;
                            }

                            readingVal = false;
                            continue;
                        }

                        valueIndex = GetInt32(val);
                        stringValues[indexToInsertAt] = uniqueValues[valueIndex];
                        indexToInsertAt++;
                        readingVal = false;
                    }
                }

                return doValues;
            }

            return new TimeseriesDataRaw
            {
                Epoch = dto.Epoch,
                Timestamps = timestamps,
                NumericValues = numericValues,
                StringValues = GetStringValues(strings),
                BinaryValues = binary,
                TagValues = GetStringValues(tags),
            };
        }

        private static bool IsArray(object value)
        {
            return value is JsonElement element && element.ValueKind == JsonValueKind.Array;
        }

        private static IEnumerable<JsonElement> EnumerateArray(object value)
        {
            return ((JsonElement)value).EnumerateArray();
        }

        private static int GetInt32(object value)
        {
            if (value is JsonElement element)
            {
                return element.GetInt32();
            }

            return (int)Convert.ToInt64(value);
        }

        private static double GetDouble(object value)
        {
            if (value is JsonElement element)
            {
                return element.GetDouble();
            }

            return Convert.ToDouble(value);
        }

        private TimeseriesDataCodeDto ConvertToDto(TimeseriesDataRaw rawData)
        {
            var numericValues = rawData.NumericValues ?? new Dictionary<string, double?[]>();
            var stringValues = rawData.StringValues ?? new Dictionary<string, string[]>();
            var tagValues = rawData.TagValues ?? new Dictionary<string, string[]>();
            var binaryValues = rawData.BinaryValues ?? new Dictionary<string, byte[][]>();

            // Insert timestamp values as deltas from previous value, where first is delta from epoch
            var timestamps = new long[rawData.Timestamps.Length];
            long last = 0;
            for (int i = 0; i < timestamps.Length; i++)
            {
                var newVal = rawData.Timestamps[i];
                var delta = newVal - last;
                timestamps[i] = delta;
                last = newVal;
            }
            
            // Insert number values as deltas from previous value
            // Logic: First a number of skipped indices, then either a list or a single value. Repeat.
            var dtoNumValues = numericValues.ToDictionary(x=> x.Key, x=> new List<object>(rawData.Timestamps.Length));
            foreach (var paramPair in numericValues)
            {
                var numParamValues = dtoNumValues[paramPair.Key];
                List<double?> segment = null;
                var skippedIndices = 0;
                double lastValueAdded = 0;
                void AddSegment()
                {
                    if (segment == null) return;
                    if (segment.Count == 1)
                    {
                        numParamValues.Add(segment[0]);
                    }
                    else
                    {
                        numParamValues.Add(segment);
                    }
                    segment = null;
                }
                for (var timeStampIndex = 0; timeStampIndex < timestamps.Length; timeStampIndex++)
                {
                    var val = paramPair.Value[timeStampIndex];
                    if (val == null)
                    {
                        skippedIndices++;
                        AddSegment();
                        continue;
                    }
                    if (segment == null)
                    {
                        numParamValues.Add(skippedIndices);
                        skippedIndices = 0;
                        segment = new List<double?>(10);
                    }

                    var valToAdd = val - lastValueAdded;
                    segment.Add(valToAdd);
                    lastValueAdded = val.Value;
                }

                AddSegment();
            }

            Dictionary<string, StringValueDto> ConvertStringValues(Dictionary<string, string[]> values)
            {
                // Insert string values as indices within a pre-collected value list
                // Logic for keys: ...
                // Logic for Values: First a number of skipped indices, then either a list or a single value. Repeat.
                var dtoStringValues = values.ToDictionary(x => x.Key, x => new StringValueDto());
                foreach (var pair in values)
                {
                    var dtoStringValue = dtoStringValues[pair.Key];
                    List<int> segment = null;
                    var skippedIndices = 0;
                    var uniqueParameterValues = new Dictionary<string, int>();

                    void AddSegment()
                    {
                        if (segment == null) return;
                        if (segment.Count == 1)
                        {
                            dtoStringValue.Values.Add(segment[0]);
                        }
                        else
                        {
                            dtoStringValue.Values.Add(segment);
                        }

                        segment = null;
                    }

                    for (var timeStampIndex = 0; timeStampIndex < timestamps.Length; timeStampIndex++)
                    {
                        var val = pair.Value[timeStampIndex];
                        if (val == null)
                        {
                            skippedIndices++;
                            AddSegment();
                            continue;
                        }

                        if (segment == null)
                        {
                            dtoStringValue.Values.Add(skippedIndices);
                            skippedIndices = 0;
                            segment = new List<int>(10);
                        }

                        if (!uniqueParameterValues.TryGetValue(val, out var valToAdd))
                        {
                            valToAdd = uniqueParameterValues.Count;
                            uniqueParameterValues.Add(val, valToAdd);
                        }

                        segment.Add(valToAdd);
                    }

                    AddSegment();
                    dtoStringValue.Keys = uniqueParameterValues.OrderBy(y=> y.Value).Select(y=> y.Key).ToList();
                }

                return dtoStringValues;
            }
            

            var dataDto = new TimeseriesDataCodeDto
            {
                Epoch = rawData.Epoch,
                Timestamps = timestamps,
                Numerics = dtoNumValues,
                Strings = ConvertStringValues(stringValues),
                Tags = ConvertStringValues(tagValues),
                Binary = binaryValues
            };


            return dataDto;
        }
        
        private class TimeseriesDataCodeDto
        {
            public long Epoch;
            
            public Dictionary<string, List<object>> Numerics;
            
            public Dictionary<string, byte[][]> Binary;
            
            public Dictionary<string, StringValueDto> Strings;
            
            public Dictionary<string, StringValueDto> Tags;

            public long[] Timestamps;
        }
        
        private class StringValueDto
        {
            [JsonPropertyName("K")]
            public List<string> Keys = new List<string>();

            [JsonPropertyName("V")]
            public List<object> Values = new List<object>();
        }
    }
}
