using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class JsonSchemaTests
    {
        //[TestMethod]
        public void When_schema_contains_refs_then_they_should_be_resolved()
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
            var schema = JsonSchema4.FromJson(data);

            //// Assert
            Assert.IsNotNull(schema.Definitions["diskDevice"]);
            Assert.IsTrue(schema.Properties["storage"].OneOf.All(p => p != null));
        }

        [TestMethod]
        public void When_deserializing_schema_then_it_should_be_read_correctly()
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
            var schema = JsonSchema4.FromJson(data);

            var x = schema.ToJson();

            //// Assert
            Assert.AreEqual(3, schema.Properties.Count);
            Assert.AreEqual(JsonObjectType.Object, schema.Type);
        }

        [TestMethod]
        public void When_deserializing_multiple_types_then_flags_should_be_set_correctly()
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
            var schema = JsonSchema4.FromJson(data);

            //// Assert
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.String));
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.Null));
        }

        [TestMethod]
        public void When_deserializing_single_type_then_flags_should_be_set_correctly()
        {
            //// Arrange
            var data = 
@"{
  ""type"": ""string""
}";

            //// Act
            var schema = JsonSchema4.FromJson(data);

            //// Assert
            Assert.IsTrue(schema.Type.HasFlag(JsonObjectType.String));
            Assert.AreEqual(JsonObjectType.String, schema.Type);
        }

        [TestMethod]
        public void When_setting_single_type_then_it_should_be_serialized_correctly()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            schema.Type = JsonObjectType.Integer;

            //// Assert
            Assert.AreEqual("integer", schema.TypeRaw.ToString());
        }

        [TestMethod]
        public void When_setting_multiple_type_then_it_should_be_serialized_correctly()
        {
            //// Arrange
            var schema = new JsonSchema4();

            //// Act
            schema.Type = JsonObjectType.Integer | JsonObjectType.Object;

            //// Assert
            var types = (JArray)schema.TypeRaw;
            Assert.AreEqual(2, types.Count);
            Assert.IsTrue(types.OfType<JValue>().Any(v => v.ToString(CultureInfo.InvariantCulture) == "integer"));
            Assert.IsTrue(types.OfType<JValue>().Any(v => v.ToString(CultureInfo.InvariantCulture) == "object"));
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
            Assert.AreEqual(schema, schema.Properties["test"].Parent);
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
    }
}
