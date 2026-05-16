using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuixStreams.Kafka.Transport.SerDes.Json
{
    public static class QuixJsonOptions
    {
        public static readonly JsonSerializerOptions Default = CreateDefault();

        public static readonly JsonSerializerOptions Indented = CreateIndented();

        private static JsonSerializerOptions CreateDefault()
        {
            return new JsonSerializerOptions
            {
                IncludeFields = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        private static JsonSerializerOptions CreateIndented()
        {
            var options = new JsonSerializerOptions(Default)
            {
                WriteIndented = true
            };

            return options;
        }
    }
}
