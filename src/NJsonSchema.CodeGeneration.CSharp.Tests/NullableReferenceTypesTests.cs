using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

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
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property_is_nullable()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRequiredObject>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });
            var schemaData = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_not_set_then_CSharp_property()
        {
            // Arrange

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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = false
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_generating_from_json_schema_property_is_optional_and_GenerateNullableOptionalProperties_is_set_then_CSharp_property()
        {
            // Arrange

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

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData("date")]
        [InlineData("date-time")]
        [InlineData("time")]
        [InlineData("time-span")]
        public async Task When_generating_from_json_schema_string_property_with_date_or_time_format(string format)
        {
            // Arrange
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

            // Act
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

            // Assert
            await VerifyHelper.Verify(code).UseParameters(format);
            CSharpCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData("date")]
        [InlineData("date-time")]
        [InlineData("time")]
        [InlineData("time-span")]
        public async Task When_generating_property_with_datetime_format_is_optional_and_GenerateNullableOptionalProperties(string format)
        {
            // Arrange
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

            // Act
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

            // Assert
            await VerifyHelper.Verify(code).UseParameters(format);
            CSharpCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_UseRequiredKeyword_is_set_then_only_required_properties_get_required_keyword(bool useRequiredKeyword)
        {
            // Arrange
            // RequiredProperty           -> gets the 'required' keyword (when enabled) and no initializer.
            // OptionalProperty           -> no 'required' keyword; keeps its '= default!' nullable initializer.
            // OptionalPropertyWithDefault -> no 'required' keyword; keeps its schema default value initializer.
            //   The value type is the key case: it only gets an initializer because of its default, so it proves
            //   non-required properties no longer lose their default values when UseRequiredKeyword is enabled.
            var schemaJson = @"
            {
                ""type"": ""object"",
                ""required"": [
                    ""RequiredProperty""
                ],
                ""properties"": {
                    ""RequiredProperty"": {
                        ""type"": ""string""
                    },
                    ""OptionalProperty"": {
                        ""type"": ""string""
                    },
                    ""OptionalPropertyWithDefault"": {
                        ""type"": ""integer"",
                        ""default"": 3
                    }
                }
            }
            ";

            var schema = await JsonSchema.FromJsonAsync(schemaJson);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.OpenApi3,
                UseRequiredKeyword = useRequiredKeyword,
                GenerateNullableReferenceTypes = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(useRequiredKeyword);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_generating_string_property_with_reference_is_optional_and_GenerateNullableOptionalProperties_is_set()
        {
            // Arrange
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

            // Act
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

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}