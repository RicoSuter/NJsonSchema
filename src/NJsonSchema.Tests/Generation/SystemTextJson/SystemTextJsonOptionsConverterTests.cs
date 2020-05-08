#if !NET46 && !NET45

using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonOptionsConverterTests
    {
        [Fact]
        public async Task SystemTextJson_WhenLowerCamelCasePropertiesAreUsed_ThenCamelCasePropertyNamesContractResolverIsUsed()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act
            var settings = SystemTextJsonUtilities.ConvertJsonOptionsToNewtonsoftSettings(options);

            // Assert
            Assert.IsType<CamelCasePropertyNamesContractResolver>(settings.ContractResolver);
            Assert.DoesNotContain(settings.Converters, c => c is StringEnumConverter);
        }

        [Fact]
        public async Task SystemTextJson_WhenEnumsAreSerializedAsStrings_ThenGlobalConverterExists()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };

            // Act
            var settings = SystemTextJsonUtilities.ConvertJsonOptionsToNewtonsoftSettings(options);

            // Assert
            Assert.Contains(settings.Converters, c => c is StringEnumConverter);
        }

        [Fact]
        public async Task SystemTextJson_WhenNamingPolicyIsNull_ThenDefaultContractResolverIsUsed()
        {
            // Arrange
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            };

            // Act
            var settings = SystemTextJsonUtilities.ConvertJsonOptionsToNewtonsoftSettings(options);

            // Assert
            Assert.IsType<DefaultContractResolver>(settings.ContractResolver);
        }
    }
}

#endif