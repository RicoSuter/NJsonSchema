using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ClassGenerationTests
    {
        public class MyClassTest
        {
            [DefaultValue("foo")]
            public string Name { get; set; }

            public DateTime DateOfBirth { get; set; }

            public int[] PrimitiveArray { get; set; }

            public Dictionary<string, int> PrimitiveDictionary { get; set; }

            public DateTime[] DateArray { get; set; }

            public Dictionary<string, DateTime> DateDictionary { get; set; }

            public Student Reference { get; set; }

            public Student[] Array { get; set; }

            public Dictionary<string, Student> Dictionary { get; set; }
        }

        public class Student : Person
        {
            public string Study { get; set; }
        }

        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [Theory]
        [InlineData(TypeScriptTypeStyle.Class, 1.8)]
        [InlineData(TypeScriptTypeStyle.Class, 2.1)]
        [InlineData(TypeScriptTypeStyle.Class, 2.7)]
        [InlineData(TypeScriptTypeStyle.Class, 4.3)]
        [InlineData(TypeScriptTypeStyle.KnockoutClass, 1.8)]
        [InlineData(TypeScriptTypeStyle.KnockoutClass, 2.1)]
        [InlineData(TypeScriptTypeStyle.KnockoutClass, 2.7)]
        [InlineData(TypeScriptTypeStyle.KnockoutClass, 4.3)]
        [InlineData(TypeScriptTypeStyle.Interface, 1.8)]
        [InlineData(TypeScriptTypeStyle.Interface, 2.1)]
        [InlineData(TypeScriptTypeStyle.Interface, 2.7)]
        [InlineData(TypeScriptTypeStyle.Interface, 4.3)]
        public async Task Verify_output(TypeScriptTypeStyle style, decimal version)
        {
            var settings = new TypeScriptGeneratorSettings
            {
                TypeStyle = style,
                TypeScriptVersion = version
            };
            var output = await PrepareAsync(settings);

            await VerifyHelper.Verify(output).UseParameters(style, version);
        }

        private static Task<string> PrepareAsync(TypeScriptGeneratorSettings settings)
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyClassTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, settings);
            var code = generator.GenerateFile("MyClass");
            return Task.FromResult(code);
        }

        [Fact]
        public async Task When_array_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                SchemaType = SchemaType.Swagger2,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_dictionary_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                SchemaType = SchemaType.Swagger2,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_object_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Properties =
                {
                    { "A", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonSchemaProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonSchemaProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonSchemaProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                SchemaType = SchemaType.Swagger2,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_export_types_is_false_dont_add_export_before_class_and_interface()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class, ExportTypes = false });

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_add_export_keyword_is_false_with_knockout_class_dont_add_export_before_class()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.KnockoutClass, ExportTypes = false });

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_GenerateConstructorInterface_then_no_interfaces_are_generated()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                GenerateConstructorInterface = false
            });

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_Knockout_class_is_generated_then_initializers_are_correct()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.KnockoutClass,
                GenerateConstructorInterface = false,
                TypeScriptVersion = 2.0m
            });

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_GenerateConstructorInterface_is_disabled_then_data_is_not_checked_and_default_initialization_is_always_exectued()
        {
            // Assert
            var schema = NewtonsoftJsonSchemaGenerator.FromType(
                typeof(MyDerivedClass),
                new NewtonsoftJsonSchemaGeneratorSettings
                {
                    GenerateAbstractProperties = true
                });

            var generator = new TypeScriptGenerator(schema);
            generator.Settings.GenerateConstructorInterface = false;
            generator.Settings.MarkOptionalProperties = true;
            generator.Settings.TypeStyle = TypeScriptTypeStyle.Class;

            // Act
            var output = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(output);
            CodeCompiler.AssertCompile(output);
        }

        [JsonConverter(typeof(JsonInheritanceConverter))]
        [KnownType(typeof(MyDerivedClass))]
        public abstract class MyBaseClass
        {
            [Required]
            public MyPropertyClass MyProperty { get; set; }
        }

        public sealed class MyDerivedClass : MyBaseClass
        {
        }

        public sealed class MyPropertyClass
        {
            public string MyStringProperty { get; set; }
        }
    }
}