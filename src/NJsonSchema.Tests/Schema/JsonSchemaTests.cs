using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class JsonSchemaTests
    {
        [TestMethod]
        public async Task When_creating_schema_without_setting_properties_then_it_is_empty()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            var data = await schema.ToJsonAsync();

            //// Assert
            Assert.AreEqual(
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#""
}", data);
            Assert.IsTrue(schema.IsAnyType);
        }

        [TestMethod]
        public async Task When_schema_contains_refs_then_they_should_be_resolved()
        {
            //// Arrange
            var data =
@"{
    ""id"": ""http://some.site.somewhere/entry-schema#"",
    ""$schema"": ""http://json-schema.org/draft-04/schema#"",
    ""description"": ""schema for an fstab entry"",
    ""type"": ""object"",
    ""required"": [ ""storage"" ],
    ""properties"": {
        ""storage"": {
            ""type"": ""object"",
            ""oneOf"": [
                { ""$ref"": ""#/definitions/diskDevice"" },
                { ""$ref"": ""#/definitions/diskUUID"" },
                { ""$ref"": ""#/definitions/nfs"" },
                { ""$ref"": ""#/definitions/tmpfs"" }
            ]
        },
        ""fstype"": {
            ""enum"": [ ""ext3"", ""ext4"", ""btrfs"" ]
        },
        ""options"": {
            ""type"": ""array"",
            ""minItems"": 1,
            ""items"": { ""type"": ""string"" },
            ""uniqueItems"": true
        },
        ""readonly"": { ""type"": ""boolean"" }
    },
    ""definitions"": {
        ""diskDevice"": {},
        ""diskUUID"": {},
        ""nfs"": {},
        ""tmpfs"": {}
    }
}
";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(data);

            //// Assert
            Assert.IsNotNull(schema.Definitions["diskDevice"]);
            Assert.IsTrue(schema.Properties["storage"].OneOf.All(p => p != null));
        }

        [TestMethod]
        public async Task When_deserializing_schema_then_it_should_be_read_correctly()
        {
            //// Arrange
            var data =
@"{
	""title"": ""Example Schema"",
	""type"": ""object"",
	""properties"": {
		""firstName"": {
			""type"": ""string""
		},
		""lastName"": {
			""type"": ""string""
		},
		""age"": {
			""description"": ""Age in years"",
			""type"": ""integer"",
			""minimum"": 0
		}
	},
	""required"": [""firstName"", ""lastName""]
}";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(data);
            var x = await schema.ToJsonAsync();

            //// Assert
            Assert.AreEqual(3, schema.Properties.Count);
            Assert.AreEqual(JsonObjectType.Object, schema.Type);
        }

        [TestMethod]
        public async Task When_deserializing_multiple_types_then_flags_should_be_set_correctly()
        {
            //// Arrange
            var data =
@"{
  ""type"": [
    ""string"",
    ""null""
  ]
}";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(data);

            //// Assert
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.String));
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.Null));
        }

        [TestMethod]
        public async Task When_deserializing_single_type_then_flags_should_be_set_correctly()
        {
            //// Arrange
            var data =
@"{
  ""type"": ""string""
}";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(data);

            //// Assert
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.String));
            Assert.AreEqual(JsonObjectType.String, schema.Type);
        }

        [TestMethod]
        public async Task When_setting_single_type_then_it_should_be_serialized_correctly()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            schema.Type = JsonObjectType.Integer;
            var data = await schema.ToJsonAsync();

            //// Assert
            Assert.IsTrue(data.Contains(@"""type"": ""integer"""));
        }

        [TestMethod]
        public async Task When_setting_multiple_type_then_it_should_be_serialized_correctly()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            schema.Type = JsonObjectType.Integer | JsonObjectType.Object;
            var data = await schema.ToJsonAsync();

            //// Assert
            Assert.IsTrue(data.Contains(
@"  ""type"": [
    ""integer"",
    ""object""
  ]"));
        }

        [TestMethod]
        public void When_adding_property_to_schema_then_parent_should_be_set()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            schema.Properties.Add("test", new JsonProperty());

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("test"));
            Assert.AreEqual(schema, schema.Properties["test"].ParentSchema);
        }

        [TestMethod]
        public void When_setting_property_required_then_the_key_should_be_added()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Properties["test"] = new JsonProperty();

            //// Act
            schema.Properties["test"].IsRequired = true;

            //// Assert
            Assert.IsTrue(schema.RequiredProperties.Contains("test"));
        }

        [TestMethod]
        public void When_setting_property_not_required_then_the_key_should_be_added()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Properties["test"] = new JsonProperty();
            schema.RequiredProperties.Add("test");

            //// Act
            schema.Properties["test"].IsRequired = false;

            //// Assert
            Assert.IsFalse(schema.RequiredProperties.Contains("test"));
        }

        [TestMethod]
        public void When_number_property_is_null_and_not_required_then_it_is_invalid()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Properties["test"] = new JsonProperty
            {
                Type = JsonObjectType.Number,
                IsRequired = false
            };

            //// Act
            var errors = schema.Validate("{ test: null }");

            //// Assert
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void When_property_matches_one_of_the_types_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty
            {
                Type = JsonObjectType.Number | JsonObjectType.Null
            };

            var token = new JObject();
            token["Foo"] = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_property_type_not_specified_then_anything_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonProperty();
            schema.Properties["Bar"] = new JsonProperty();

            var token = new JObject();
            token["Foo"] = new JValue(5);
            token["Bar"] = new JValue("Bar");
            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.AreEqual(0, errors.Count());
        }

        [TestMethod]
        public void When_DateTimeOffset_is_validated_then_it_should_not_throw()
        {
            //// Arrange
            var schema = new JsonSchema4
            {
                Type = JsonObjectType.String
            };

            var token = new JValue(System.DateTimeOffset.Now);

            try
            {
                //// Act
                schema.Validate(token);
            }
            catch
            {
                //// Assert
                Assert.Fail("Validating JToken with a DateTimeOffset value threw an exception.");
            }
        }
    }
}
