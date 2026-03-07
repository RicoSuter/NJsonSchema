using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class ObsoleteTests
    {
        public class ObsoletePropertyTestClass
        {
            [Obsolete]
            public string Property { get; set; }
        }

        public class ObsoletePropertyWithMessageTestClass
        {
            [Obsolete("Reason property is \"obsolete\"")]
            public string Property { get; set; }
        }

        [Obsolete]
        public class ObsoleteTestClass
        {
            public string Property { get; set; }
        }

        [Obsolete(@"Reason class is ""obsolete""")]
        public class ObsoleteWithMessageTestClass
        {
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_property_is_obsolete_then_obsolete_attribute_is_rendered()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObsoletePropertyTestClass>();
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_is_obsolete_with_a_message_then_obsolete_attribute_with_a_message_is_rendered()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObsoletePropertyWithMessageTestClass>();
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_is_obsolete_then_obsolete_attribute_is_rendered()
        {
            // Arrange
#pragma warning disable 612
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObsoleteTestClass>();
#pragma warning restore 612
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_is_obsolete_with_a_message_then_obsolete_attribute_with_a_message_is_rendered()
        {
            // Arrange
#pragma warning disable 618
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObsoleteWithMessageTestClass>();
#pragma warning restore 618
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_with_base_class_has_deprecated_in_allOf_then_obsolete_attribute_is_rendered()
        {
            // Arrange - simulates the schema structure where deprecated is in an allOf sub-schema
            var json = @"{
                ""type"": ""object"",
                ""definitions"": {
                    ""BaseDto"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""string"" }
                        }
                    },
                    ""DerivedDto"": {
                        ""allOf"": [
                            { ""$ref"": ""#/definitions/BaseDto"" },
                            {
                                ""type"": ""object"",
                                ""x-deprecated"": true,
                                ""x-deprecatedMessage"": ""Has been replaced by flows."",
                                ""properties"": {
                                    ""url"": { ""type"": ""string"" }
                                }
                            }
                        ]
                    }
                },
                ""properties"": {
                    ""item"": { ""$ref"": ""#/definitions/DerivedDto"" }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            // Act
            var code = generator.GenerateFile();

            // Assert
            Assert.Contains("[System.Obsolete(\"Has been replaced by flows.\")]", code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_class_with_base_class_has_deprecated_without_message_in_allOf_then_obsolete_attribute_is_rendered()
        {
            // Arrange
            var json = @"{
                ""type"": ""object"",
                ""definitions"": {
                    ""BaseDto"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""string"" }
                        }
                    },
                    ""DeprecatedDto"": {
                        ""allOf"": [
                            { ""$ref"": ""#/definitions/BaseDto"" },
                            {
                                ""type"": ""object"",
                                ""x-deprecated"": true,
                                ""properties"": {
                                    ""name"": { ""type"": ""string"" }
                                }
                            }
                        ]
                    }
                },
                ""properties"": {
                    ""item"": { ""$ref"": ""#/definitions/DeprecatedDto"" }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            // Act
            var code = generator.GenerateFile();

            // Assert
            Assert.Contains("[System.Obsolete]", code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}
