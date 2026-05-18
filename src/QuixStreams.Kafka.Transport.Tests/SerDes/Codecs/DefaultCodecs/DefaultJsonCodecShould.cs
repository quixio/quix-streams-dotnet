using System.Text;
using FluentAssertions;
using QuixStreams.Kafka.Transport.SerDes.Codecs.DefaultCodecs;
using QuixStreams.Kafka.Transport.Tests.Helpers;
using Xunit;

namespace QuixStreams.Kafka.Transport.Tests.SerDes.Codecs.DefaultCodecs
{
    public class DefaultJsonCodecShould
    {
        [Fact]
        public void Serialize_Deserialize_ShouldReturnInputModel()
        {
            // Arrange
            var codec = new DefaultJsonCodec<TestModel>();

            var model = TestModel.Create();

            // Act
            var serialized = codec.Serialize(model);

            var deserialized = codec.Deserialize(serialized);

            // Asssert

            deserialized.Should().BeEquivalentTo(model);
            
        }

        [Fact]
        public void Serialize_ShouldKeepFieldsEnumsAndOmitDefaultValues()
        {
            // Arrange
            var codec = new DefaultJsonCodec<JsonShapeModel>();

            var model = new JsonShapeModel
            {
                Name = "stream-a",
                Mode = JsonShapeMode.Ready,
                Count = 0
            };

            // Act
            var serialized = codec.Serialize(model);

            // Assert
            Encoding.UTF8.GetString(serialized).Should().Be(@"{""Name"":""stream-a"",""Mode"":""Ready""}");
        }

        private class JsonShapeModel
        {
            public string Name;
            public JsonShapeMode Mode;
            public int Count;
        }

        private enum JsonShapeMode
        {
            Unknown,
            Ready
        }
    }
}
