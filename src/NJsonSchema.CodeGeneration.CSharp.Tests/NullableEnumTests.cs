using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class NullableEnumTests
    {
        [Fact]
        public async Task When_Swagger2_enum_property_is_not_required_then_it_is_nullable()
        {
            var json =
            @"{
                ""properties"": {
                    ""sex"": {
                        ""$ref"": ""#/definitions/Sex""
                    }
                },
                ""definitions"": {
                    ""Sex"": {
                        ""type"": ""string"",
                        ""enum"": [
                            ""male"",
                            ""female""
                        ]
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2,
                ClassStyle = CSharpClassStyle.Poco
            });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains(@"public Sex? Sex", code);
        }
    }
}
