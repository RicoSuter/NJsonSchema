using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class JsonIgnoreAttributesTests
    {
        [Fact]
        public async Task When_using_SystemTextJson_JsonIgnoreAttributes_are_generated_based_on_optionality()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
                ""type"": ""object"",
                ""required"": [""requiredValue"",""requiredNullableValue"",""requiredRef""],
                ""properties"": {
                    ""requiredValue"": { ""type"": ""integer"", ""format"": ""int32"" },
                    ""requiredNullableValue"": { ""type"": [""integer"", ""null""], ""format"": ""int32"" },
                    ""requiredRef"": { ""type"": ""string"" },
                    ""optionalValue"": { ""type"": ""integer"", ""format"": ""int32"" },
                    ""optionalNullableValue"": { ""type"": [""integer"", ""null""], ""format"": ""int32"" },
                    ""optionalRef"": { ""type"": ""string"" }
                }
            }");

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                //GenerateNullableReferenceTypes = true,
                //GenerateOptionalPropertiesAsNullable = true,
            });

            static string Normalized(string str) =>
                Regex.Replace(str, @"\s+", " ");

            //// Act
            var code = generator.GenerateFile("MyClass");

            /// Assert
            Assert.Contains(
                Normalized(@"public int OptionalValue {"),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""requiredValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""requiredNullableValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""requiredRef"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalNullableValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalRef"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
                "),
                Normalized(code)
            );
        }

        [Fact]
        public async Task When_using_SystemTextJson_and_RequiredPropertiesMustBeDefined_is_false_JsonIgnoreAttributes_are_not_generated_for_required_properties()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
                ""type"": ""object"",
                ""required"": [""required""],
                ""properties"": {
                    ""required"": { ""type"": ""string"" }
                }
            }");

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                RequiredPropertiesMustBeDefined = false
            });

            static string Normalized(string str) =>
                Regex.Replace(str, @"\s+", " ");

            //// Act
            var code = generator.GenerateFile("MyClass");

            /// Assert
            Assert.DoesNotContain(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonIgnore
                "),
                Normalized(code)
            );
        }

        [Fact]
        public async Task When_using_SystemTextJson_and_RequiredPropertiesMustBeDefined_is_false_JsonIgnoreAttributes_are_still_generated_for_optional_properties()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
                ""type"": ""object"",
                ""required"": [],
                ""properties"": {
                    ""optionalRef"": { ""type"": ""string"" },
                    ""optionalValue"": { ""type"": ""integer"", ""format"": ""int32"" },
                    ""optionalNullableValue"": { ""type"": [""integer"", ""null""], ""format"": ""int32"" }
                }
            }");

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                RequiredPropertiesMustBeDefined = false
            });

            static string Normalized(string str) =>
                Regex.Replace(str, @"\s+", " ");

            //// Act
            var code = generator.GenerateFile("MyClass");

            /// Assert
            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalRef"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
                "),
                Normalized(code)
            );

            Assert.Contains(
                Normalized(@"
                    [System.Text.Json.Serialization.JsonPropertyName(""optionalNullableValue"")]
                    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
                "),
                Normalized(code)
            );
        }
    }
}
