using System.Linq;
using System.Text;
using FluentAssertions;
using QuixStreams.Kafka.Transport.SerDes.Codecs.DefaultCodecs;
using QuixStreams.Kafka.Transport.SerDes.Legacy.MessageValue;
using Xunit;

namespace QuixStreams.Kafka.Transport.Tests.SerDes.Legacy.MessageValue
{
    public class TransportPackageValueCodecJSONShould
    {
        [Fact]
        public void Serialize_ShouldKeepJsonPayloadAsRawJson()
        {
            // Arrange
            var payload = Encoding.UTF8.GetBytes(@"{""value"":1}");
            var value = new TransportPackageValue(payload, CodecBundle.WellKnownCodecBundles.Default);

            // Act
            var serialized = TransportPackageValueCodecJSON.Serialize(value);

            // Assert
            Encoding.UTF8.GetString(serialized).Should().Contain(@"""V"":{""value"":1}");
        }

        [Fact]
        public void Serialize_ThenDeserialize_ShouldReturnInputPayloadAndCodecBundle()
        {
            // Arrange
            var payload = Encoding.UTF8.GetBytes(@"{""value"":1}");
            var value = new TransportPackageValue(payload, CodecBundle.WellKnownCodecBundles.Default);

            // Act
            var deserialized = TransportPackageValueCodecJSON.Deserialize(TransportPackageValueCodecJSON.Serialize(value));

            // Assert
            deserialized.Value.ToArray().Should().Equal(payload);
            deserialized.CodecBundle.ModelKey.Should().Be(CodecBundle.WellKnownCodecBundles.Default.ModelKey);
            deserialized.CodecBundle.CodecId.Should().Be(CodecBundle.WellKnownCodecBundles.Default.CodecId);
        }
    }
}
