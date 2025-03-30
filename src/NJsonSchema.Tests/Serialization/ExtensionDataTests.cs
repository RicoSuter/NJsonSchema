﻿using System.ComponentModel.DataAnnotations;
using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Serialization
{
    public class ExtensionDataTests
    {
        [Fact]
        public async Task When_definition_entry_is_null_then_it_is_deserialized_correctly()
        {
            // Arrange
            var json = @"{ ""definitions"": { ""abc"": null } }";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var json2 = schema.ToJson();

            // Assert
            Assert.False(schema.Definitions.ContainsKey("abc"));
            Assert.DoesNotContain("abc", json2);
        }

        [Fact]
        public void When_schema_has_extension_data_property_then_property_is_in_serialized_json()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.ExtensionData = new Dictionary<string, object>
            {
                { "Test", 123 }
            };

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.Contains(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""Test"": 123
}".Replace("\r\n", Environment.NewLine), json);
        }

        [Fact]
        public async Task When_json_schema_contains_unknown_data_then_extension_data_is_filled()
        {
            // Arrange
            var json =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""Test"": 123
}";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            // Assert
            Assert.Equal((long)123, schema.ExtensionData["Test"]);
        }

        [Fact]
        public async Task When_no_extension_data_is_available_then_property_is_null()
        {
            // Arrange
            var json =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
}";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);

            // Assert
            Assert.Null(schema.ExtensionData);
        }

        [JsonSchemaExtensionData("MyClass", 123)]
        public class MyTest
        {
            [JsonSchemaExtensionData("Foo", 2)]
            [JsonSchemaExtensionData("Bar", 3)]
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_extension_data_attribute_is_used_on_class_then_extension_data_property_is_set()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyTest>();

            // Assert
            Assert.Equal(123, schema.ExtensionData["MyClass"]);
        }

        [Fact]
        public async Task When_extension_data_attribute_is_used_on_property_then_extension_data_property_is_set()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyTest>();

            // Assert
            Assert.Equal(2, schema.Properties["Property"].ExtensionData["Foo"]);
            Assert.Equal(3, schema.Properties["Property"].ExtensionData["Bar"]);
        }

        public class MyCustomExtensionAttribute : ValidationAttribute, IJsonSchemaExtensionDataAttribute
        {
            public MyCustomExtensionAttribute()
            {
                this.Key = "My custom key";
                this.Value = "My custom logic";
            }

            public string Key { get; }

            public object Value { get; }

            public IReadOnlyDictionary<string, object> ExtensionData => new Dictionary<string, object>
            {
                { Key, Value }
            };

            public override bool IsValid(object value)
            {
                return false;
            }
        }

        [MyCustomExtension()]
        public class MyCustomAttributeTest
        {
            [MyCustomExtension]
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_extension_data_interface_is_used_on_property_then_extension_data_property_is_set()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyCustomAttributeTest>();

            // Assert
            Assert.Equal("My custom logic", schema.Properties["Property"].ExtensionData["My custom key"]);
            Assert.Equal("My custom logic", schema.Properties["Property"].ExtensionData["My custom key"]);
        }

        [Fact]
        public async Task When_reference_references_schema_in_custom_properties_then_the_references_are_resolved()
        {
            // Arrange
            var json =
                @"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""minItems"": 1,
  ""additionalProperties"": false,
  ""items"": {
    ""maxProperties"": 1,
    ""minProperties"": 1,
    ""additionalProperties"": false,
    ""properties"": {
      ""Ok"": {
        ""$ref"": ""#/messages/Ok""
      }
    }
  },
  ""components"": {
    ""Foo"": true,
    ""Bar"": {},
    ""Id"": {
      ""type"": ""integer"",
      ""maximum"": 4294967295.0,
      ""minimum"": 0.0
    },
    ""IdMessage"": {
      ""maxProperties"": 1,
      ""minProperties"": 1,
      ""additionalProperties"": false,
      ""required"": [
        ""Id""
      ],
      ""properties"": {
        ""Id"": {
          ""$ref"": ""#/components/Id""
        }
      }
    }
  },
  ""messages"": {
    ""Ok"": {
      ""type"": ""object"",
      ""anyOf"": [
        {
          ""$ref"": ""#/components/IdMessage""
        }
      ]
    }
  }
}";

            // Act
            var schema = await JsonSchema.FromJsonAsync(json);
            var json2 = schema.ToJson();

            // Assert
            Assert.Equal(json.Replace("\r", string.Empty), json2.Replace("\r", string.Empty));
        }
    }
}
