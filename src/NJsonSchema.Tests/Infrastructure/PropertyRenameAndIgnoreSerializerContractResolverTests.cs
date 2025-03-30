﻿using Newtonsoft.Json;
using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Infrastructure
{
    public class PropertyRenameAndIgnoreSerializerContractResolverTests
    {
        [Fact]
        public void When_property_is_renamed_then_it_does_not_land_in_extension_data()
        {
            // Arrange
            var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            resolver.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
            resolver.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");

            var json = "{ \"readOnly\": true, \"nullable\": true, \"additionalProperties\": { \"nullable\": true } }";

            // Act
            var obj = JsonSchemaSerialization.FromJson<JsonSchemaProperty>(json, resolver);

            // Assert
            Assert.True(obj.IsReadOnly);
            Assert.True(obj.IsNullableRaw);
            Assert.True(obj.AdditionalPropertiesSchema.IsNullableRaw);
        }

        public class MyClass
        {
            [JsonProperty("foo")]
            public string Foo { get; set; }
        }

        [Fact]
        public void When_property_is_renamed_then_json_is_correct()
        {
            // Arrange
            var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            resolver.RenameProperty(typeof(MyClass), "foo", "bar");

            var obj = new MyClass();
            obj.Foo = "abc";

            // Act
            var json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ContractResolver = resolver });
            obj = JsonConvert.DeserializeObject<MyClass>(json, new JsonSerializerSettings { ContractResolver = resolver });

            // Assert
            Assert.Contains("bar", json);
            Assert.Contains("abc", obj.Foo);
        }

        public class ClassWithDoubleProperties
        {
            [JsonProperty("schema")]
            public JsonSchema Schema { get; set; }

            [JsonProperty("definitions1")]
            public Dictionary<string, JsonSchema> Definitions1 => Definitions2;

            [JsonProperty("definitions2")]
            public Dictionary<string, JsonSchema> Definitions2 { get; set; } = new Dictionary<string, JsonSchema>();
        }

        [Fact]
        public void When_property_is_ignored_then_refs_ignore_it()
        {
            // Arrange
            var contractResolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            contractResolver.IgnoreProperty(typeof(ClassWithDoubleProperties), "definitions1");

            var schema = new JsonSchema
            {
                Type = JsonObjectType.Object
            };
            var foo = new ClassWithDoubleProperties
            {
                Schema = new JsonSchema { Reference = schema },
                Definitions1 =
                {
                    { "Bar", schema }
                }
            };

            // Act
            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(foo, false, contractResolver);
            var json = JsonConvert.SerializeObject(foo, Formatting.Indented, new JsonSerializerSettings { ContractResolver = contractResolver });

            // Assert
            Assert.Contains("#/definitions2/Bar", json);
            Assert.DoesNotContain("#/definitions1/Bar", json);
        }
    }
}
