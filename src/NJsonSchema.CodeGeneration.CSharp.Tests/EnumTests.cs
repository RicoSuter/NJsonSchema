using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class EnumTests
    {
        [Fact]
        public async Task When_enum_has_no_type_then_enum_is_generated()
        {
            //// Arrange
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
            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public enum MyClassCategory", code);
        }

        [Fact]
        public async Task When_enum_has_no_type_then_enum_is_generated_with_flags()
        {
            //// Arrange
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

            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { EnforceFlagEnums = true });

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("[System.Flags]", code);
            Assert.Contains("Commercial = 1,", code);
            Assert.Contains("Residential = 2,", code);
            Assert.Contains("Government = 4,", code);
            Assert.Contains("Military = 8,", code);
            Assert.Contains("Foreigngovernment = 16,", code);
        }

        [Fact]
        public async Task When_enum_name_contains_colon_then_it_is_removed_and_next_word_converted_to_upper_case()
        {
            //// Arrange
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

            var schema = await JsonSchema4.FromJsonAsync(json);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("PullrequestUpdated = 0,", code);
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
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyStringEnumListTest>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter)", code);
        }

        [Fact]
        public async Task When_enum_is_nullable_then_StringEnumConverter_is_set()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyStringEnumListTest>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]", code);
        }

        public enum SomeEnum { Thing1, Thing2 }

        public class SomeClass
        {
            public int SomeProperty { get; set; }
            public SomeEnum[] SomeEnums { get; set; }
        }

        [Fact]
        public async Task When_class_has_enum_array_property_then_enum_name_is_preserved()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<SomeClass>(new JsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("SomeEnum", code);
            Assert.DoesNotContain("Anonymous", code);
        }

        [Fact]
        public async Task When_type_name_hint_has_generics_then_they_are_converted()
        {
            /// Arrange
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
            /// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            var settings = new CSharpGeneratorSettings();
            var generator = new CSharpGenerator(schema, settings);

            var code = generator.GenerateFile("Foo");

            /// Assert
            Assert.Contains("public enum FirstMetdodOfMetValueGroupChar", code);
        }
    }
}
