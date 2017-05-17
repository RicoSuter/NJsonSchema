using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class CSharpEnumGeneratorTests
    {
        [TestMethod]
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
            Assert.IsTrue(code.Contains("public enum MyClassCategory"));
        }

        [TestMethod]
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
            Assert.IsTrue(code.Contains("PullrequestUpdated = 0,"));
        }

        public class MyStringEnumListTest
        {
            public List<MyStringEnum> Enums { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum MyStringEnum
        {
            Foo = 0,
            Bar = 1
        }

        [TestMethod]
        public async Task When_enum_list_uses_string_enums_then_ItemConverterType_is_set()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyStringEnumListTest>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter)"));
        }

        public enum SomeEnum { Thing1, Thing2 }

        public class SomeClass
        {
            public int SomeProperty { get; set; }
            public SomeEnum[] SomeEnums { get; set; }
        }

        [TestMethod]
        public async Task When_class_has_enum_array_property_then_enum_name_is_preserved()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<SomeClass>(new JsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("SomeEnum"));
            Assert.IsFalse(code.Contains("Anonymous"));
        }
    }
}
