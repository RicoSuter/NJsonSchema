﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class EnumTests
    {
        [Fact]
        public async Task When_enum_has_no_type_then_enum_is_generated()
        {
            // Arrange
            var json =
                @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""category"" : {
                        ""enum"" : [
                            ""commercial"",
                            ""residential""
                        ]
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_has_no_type_then_enum_is_generated_with_flags()
        {
            // Arrange
            var json =
                @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""category"" : {
                        ""enum"" : [
                            ""commercial"",
                            ""residential"",
                            ""government"",
                            ""military"",
                            ""foreigngovernment""
                        ]
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { EnforceFlagEnums = true });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_name_contains_colon_then_it_is_removed_and_next_word_converted_to_upper_case()
        {
            // Arrange
            var json = @"
            {
                ""type"": ""object"",
                ""properties"": {
                    ""event"": {
                        ""type"": ""string"",
                        ""enum"": [
                        ""pullrequest:updated"",
                        ""repo:commit_status_created"",
                        ""repo:updated"",
                        ""issue:comment_created"",
                        ""project:updated"",
                        ""pullrequest:rejected"",
                        ""pullrequest:fulfilled"",
                        ""repo:imported"",
                        ""repo:deleted"",
                        ""pullrequest:comment_created"",
                        ""pullrequest:comment_deleted"",
                        ""repo:fork"",
                        ""issue:created"",
                        ""repo:commit_comment_created"",
                        ""pullrequest:approved"",
                        ""repo:commit_status_updated"",
                        ""pullrequest:comment_updated"",
                        ""issue:updated"",
                        ""pullrequest:unapproved"",
                        ""pullrequest:created"",
                        ""repo:push""
                        ],
                        ""description"": ""The event identifier.""
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        public class MyStringEnumListTest
        {
            public List<MyStringEnum> Enums { get; set; }

            public MyStringEnum? NullableEnum { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum MyStringEnum
        {
            Foo = 0,
            Bar = 1
        }

        [Fact]
        public async Task When_enum_list_uses_string_enums_then_ItemConverterType_is_set()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyStringEnumListTest>();
            var data = schema.ToJson();
            var generator =
                new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_is_nullable_then_StringEnumConverter_is_set()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyStringEnumListTest>();
            var data = schema.ToJson();
            var generator =
                new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        public enum SomeEnum
        {
            Thing1,
            Thing2
        }

        public class SomeClass
        {
            public int SomeProperty { get; set; }
            public SomeEnum[] SomeEnums { get; set; }
        }

        [Fact]
        public async Task When_class_has_enum_array_property_then_enum_name_is_preserved()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<SomeClass>(new NewtonsoftJsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            // Act
            var generator =
                new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_type_name_hint_has_generics_then_they_are_converted()
        {
            // Arrange
            var json = @"
{
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/FirstMetdod<MetValue>""
        }
    },
    ""definitions"": {
        ""FirstMetdod<MetValue>"": {
            ""type"": ""object"",
            ""properties"": {
            ""GroupChar"": {
                ""type"": ""string"",
                    ""enum"": [
                    ""A"",
                    ""B"",
                    ""C"",
                    ""D""
                    ]
                }
            }
        }
    }
}";
            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_property_is_not_required_in_Swagger2_then_it_is_nullable()
        {
            // Arrange
            var json =
                @"{
    ""type"": ""object"",
    ""required"": [
        ""name"",
        ""photoUrls""
    ],
    ""properties"": {
        ""status"": {
            ""type"": ""string"",
            ""description"": ""pet status in the store"",
            ""enum"": [
                ""available"",
                ""pending"",
                ""sold""
            ]
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { SchemaType = SchemaType.Swagger2 });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_contains_operator_convert_to_string_equivalent()
        {
            ////Arrange
            var json = @"{
            ""type"": ""object"",
                ""properties"": {
                    ""foo"": {
                        ""$ref"": ""#/definitions/OperatorTestEnum""
                    }
                },
                ""definitions"": {
                    ""OperatorTestEnum"": {
                        ""type"": ""string"",
                            ""description"": ""The operator between the field and operand."",
                            ""enum"": [
                                ""="",
                                ""!="",
                                "">"",
                                ""<"",
                                "">="",
                                ""<="",
                                ""in"",
                                ""not in"",
                                null,
                                ""~="",
                                ""is"",
                                ""is not""
                            ]
                    }   
                }
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_starts_with_plus_or_minus_convert_to_string_equivalent()
        {
            ////Arrange
            var json = @"{
            ""type"": ""object"",
                ""properties"": {
                    ""foo"": {
                        ""$ref"": ""#/definitions/PlusMinusTestEnum""
                    }
                },
                ""definitions"": {
                    ""PlusMinusTestEnum"": {
                        ""type"": ""string"",
                            ""description"": ""Add or subtract from property"",
                            ""enum"": [
                                ""-Foo"",
                                ""+Foo""
                            ]
                    }   
                }
            }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_array_item_enum_is_not_referenced_then_type_name_hint_is_property_name()
        {
            // Arrange
            var json = @"
{
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/NewOrderModel""
        }
    },
    ""definitions"": {
         ""NewOrderModel"": {
            ""type"": ""object"",
            ""properties"": {
            ""id"": {
                ""format"": ""int32"",
                ""type"": ""integer""
            },
            ""name"": {
                ""type"": ""string""
            },
            ""status"": {
                ""uniqueItems"": false,
                ""type"": ""array"",
                ""items"": {
                    ""enum"": [
                        ""Finished"",
                        ""InProgress""
                    ],
                    ""type"": ""string""
                    }
                }
            }
        }
    }
}";
            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_is_integer_flags_it_should_use_declared_values()
        {
            // Arrange
            var json = @"
{
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/FlagsTestEnum""
        }
    },
    ""definitions"": {
       ""FlagsTestEnum"": {
           ""type"": ""integer"",
           ""description"": """",
           ""x-enumFlags"": true,
           ""x-enumNames"": [
             ""None"",
             ""FirstBit"",
             ""SecondBit"",
             ""ThirdBit"",
             ""FirstAndSecondBits"",
             ""All""
           ],
        ""enum"": [
          0,
          1,
          2,
          4,
          3,
          7
        ]
      }
    }
}";
            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }


        [Fact]
        public async Task When_enum_is_nullable_not_required_it_should_be_nullable_with_converter()
        {
            // Arrange
            var json = @"
{
    ""type"": ""object"",
    ""properties"": {
        ""myProperty"": {
              ""type"": [
                ""string"",
                ""null""
              ],
              ""enum"": [
                ""value1"",
                ""value2"",
                ""value3"",
                ""NONE"",
                null
              ]
        }
    }
}";
            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings { EnforceFlagEnums = true };
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            //Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_is_nullable_required_it_should_be_nullable_with_converter()
        {
            // Arrange
            var json = @"
{
    ""type"": ""object"",
    ""required"": [ ""myProperty"" ],
    ""properties"": {
        ""myProperty"": {
              ""type"": [
                ""string"",
                ""null""
              ],
              ""enum"": [
                ""value1"",
                ""value2"",
                ""value3"",
                ""NONE"",
                null
              ]
        }
    }
}";
            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings { EnforceFlagEnums = true };
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_is_nullable_and_has_default_then_question_mark_is_omitted()
        {
            // Arrange
            var json =
                @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""category"" : {
                        ""type"" : ""string"",
                        ""x-nullable"" : true,
                        ""default"" : ""commercial"",
                        ""enum"" : [
                            ""commercial"",
                            ""residential""
                        ]
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true,
                GenerateOptionalPropertiesAsNullable = true,
            });

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_enum_has_a_format_then_enum_is_generated_with_correct_basetype()
        {
            // Arrange
            var json = @"
{
    ""properties"": {
        ""foo"": {
            ""$ref"": ""#/definitions/ManyValuesTestEnum""
        }
    },
    ""definitions"": {
        ""ManyValuesTestEnum"": {
            ""type"": ""integer"",
            ""format"": ""int64"",
            ""x-enumNames"": [
                ""None"",
                ""FirstBit""
            ],
            ""enum"": [
                0,
                1
            ]
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            // Act
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}
