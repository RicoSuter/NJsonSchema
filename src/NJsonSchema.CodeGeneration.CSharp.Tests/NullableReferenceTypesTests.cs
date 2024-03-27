using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class NullableReferenceTypesTests
    {
        private class ClassWithRequiredObject
        {
            public object Property { get; set; }

            [Required]
            [Newtonsoft.Json.JsonProperty("property2", Required = Newtonsoft.Json.Required.Always)]
            public object Property2 { get; set; }
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableReferenceTypes_is_not_set_then_CSharp_property_is_not_nullable()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object Property { get; set; }", code);
            Assert.Contains("public object Property2 { get; set; }", code);
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property_is_nullable()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object? Property { get; set; } = default!;", code);
            Assert.Contains("public object Property2 { get; set; } = default!;", code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_not_set_then_CSharp_property()
        {
            //// Arrange

            // CSharpGenerator `new object()`  adds = new object() initializer to property only if it's explicitly marked
            // as having `type: object` in json schema
            var schemaJson = @" 
            {
                ""type"": ""object"",
                ""required"": [
                    ""property2""
                ],
                ""properties"": {
                    ""Property"": {
                        ""x-nullable"": true,
                        ""type"": ""object""
                    },
                    ""property2"": {
                        ""type"": ""object""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object Property { get; set; }", code);
            Assert.Contains("public object Property2 { get; set; } = new object();", code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property()
        {
            //// Arrange

            // CSharpGenerator `new object()`  adds = new object() initializer to property only if it's explicitly marked
            // as having `type: object` in json schema
            var schemaJson = @" 
            {
                ""type"": ""object"",
                ""required"": [
                    ""property2""
                ],
                ""properties"": {
                    ""Property"": {
                        ""x-nullable"": true,
                        ""type"": ""object""
                    },
                    ""property2"": {
                        ""type"": ""object""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var schemaData = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public object? Property { get; set; } = default!;", code);
            Assert.Contains("public object Property2 { get; set; } = new object();", code);
        }

        [Theory]
        [InlineData("date")]
        [InlineData("date-time")]
        [InlineData("time")]
        [InlineData("time-span")]
        public async Task When_generating_from_json_schema_string_property_with_date_or_time_format_is_optional_and_GenerateNullableOptionalProperties_is_not_set_then_CSharp_property(string format)
        {
            //// Arrange
            var schemaJson = @"
            {
                ""type"": ""object"",
                ""required"": [
                    ""required""
                ],
                ""properties"": {
                    ""required"": {
                        ""type"": ""string"",
                        ""format"": """ + format + @"""
                    },
                    ""optional"": {
                        ""type"": ""string"",
                        ""format"": """ + format + @"""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                DateType = "string",
                DateTimeType = "string",
                TimeType = "string",
                TimeSpanType = "string",
                GenerateNullableReferenceTypes = false,
                GenerateOptionalPropertiesAsNullable = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public string Required { get; set; }", code);
            Assert.Contains("public string Optional { get; set; }", code);
        }

        [Theory]
        [InlineData("date")]
        [InlineData("date-time")]
        [InlineData("time")]
        [InlineData("time-span")]
        public async Task When_generating_from_json_schema_string_property_with_date_or_time_format_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property(string format)
        {
            //// Arrange
            var schemaJson = @"
            {
                ""type"": ""object"",
                ""required"": [
                    ""required""
                ],
                ""properties"": {
                    ""required"": {
                        ""type"": ""string"",
                        ""format"": """ + format + @"""
                    },
                    ""optional"": {
                        ""type"": ""string"",
                        ""format"": """ + format + @"""
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                DateType = "string",
                DateTimeType = "string",
                TimeType = "string",
                TimeSpanType = "string",
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public string Required { get; set; } = default!;", code);
            Assert.Contains("public string? Optional { get; set; } = default!;", code);
        }

        [Fact]
        public async Task
            When_generating_from_json_schema_string_property_with_reference_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property()
        {
            //// Arrange
            var schemaJson = @"
            {
                ""type"": ""object"",
                ""required"": [
                    ""required""
                ],
                ""properties"": {
                    ""required"": { ""$ref"": ""#/$defs/requiredString"" },
                    ""optional"": { ""$ref"": ""#/$defs/optionalString"" }
                },
                ""$defs"": {
                    ""requiredString"": { ""type"": ""string"" },
                    ""optionalString"": { ""type"": ""string"" }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                DateType = "string",
                DateTimeType = "string",
                TimeType = "string",
                TimeSpanType = "string",
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public string Required { get; set; } = default!;", code);
            Assert.Contains("public string? Optional { get; set; } = default!;", code);
        }
    }
}