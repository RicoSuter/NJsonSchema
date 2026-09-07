namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class CustomStringFormatTests
    {
        private const string Json =
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""stamp"": { ""type"": ""string"", ""format"": ""date-time-offset"" }
                }
            }";

        [Fact]
        public async Task When_string_format_is_mapped_then_the_mapped_type_is_generated()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };
            settings.CustomStringFormatTypes["date-time-offset"] = "System.DateTimeOffset";

            // Act
            var code = new CSharpGenerator(schema, settings).GenerateFile("MyClass");

            // Assert
            Assert.Contains("public System.DateTimeOffset Stamp { get; set; }", code);
        }

        [Fact]
        public async Task When_mapped_string_format_is_nullable_then_the_mapped_type_is_nullable()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(
                @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""stamp"": { ""type"": [ ""string"", ""null"" ], ""format"": ""date-time-offset"" }
                    }
                }");

            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };
            settings.CustomStringFormatTypes["date-time-offset"] = "System.DateTimeOffset";

            // Act
            var code = new CSharpGenerator(schema, settings).GenerateFile("MyClass");

            // Assert
            Assert.Contains("public System.DateTimeOffset? Stamp { get; set; }", code);
        }

        [Fact]
        public async Task When_string_format_is_not_mapped_then_string_is_generated()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);
            var settings = new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns" };

            // Act
            var code = new CSharpGenerator(schema, settings).GenerateFile("MyClass");

            // Assert
            Assert.Contains("public string Stamp { get; set; }", code);
        }

        [Fact]
        public async Task When_a_natively_handled_format_is_mapped_then_the_mapping_is_ignored()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(
                @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""stamp"": { ""type"": ""string"", ""format"": ""date-time"" }
                    }
                }");

            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                DateTimeType = "System.DateTime"
            };
            settings.CustomStringFormatTypes["date-time"] = "System.DateTimeOffset";

            // Act
            var code = new CSharpGenerator(schema, settings).GenerateFile("MyClass");

            // Assert
            Assert.Contains("public System.DateTime Stamp { get; set; }", code);
        }
    }
}
