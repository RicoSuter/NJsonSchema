using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ConstTests
    {
        [Fact]
        public async Task When_property_has_const_string_then_interface_has_literal_type()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": {
                        ""const"": ""person""
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("cmdType: \"person\"", code);
        }

        [Fact]
        public async Task When_property_has_const_integer_then_literal_type_is_generated()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""version"": {
                        ""const"": 42
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("version: 42", code);
        }

        [Fact]
        public async Task When_property_has_const_boolean_then_literal_type_is_generated()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""isActive"": {
                        ""const"": true
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("isActive: true", code);
        }

        [Fact]
        public async Task When_property_has_const_string_with_class_style_then_readonly_property_is_generated()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": {
                        ""const"": ""person""
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("\"person\"", code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_has_const_via_ref_then_literal_type_is_generated()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": { ""$ref"": ""#/definitions/Cmd"" }
                },
                ""definitions"": {
                    ""Cmd"": { ""const"": ""person"" }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("cmdType: \"person\"", code);
        }

        [Fact]
        public async Task When_property_has_const_inside_multi_allOf_then_literal_type_is_generated()
        {
            // Only ActualTypeSchema dives into multi-item allOf to find const.
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": {
                        ""allOf"": [
                            { ""$ref"": ""#/definitions/Base"" },
                            { ""const"": ""person"" }
                        ]
                    }
                },
                ""definitions"": {
                    ""Base"": { ""type"": ""string"" }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("cmdType: \"person\"", code);
        }
    }
}
