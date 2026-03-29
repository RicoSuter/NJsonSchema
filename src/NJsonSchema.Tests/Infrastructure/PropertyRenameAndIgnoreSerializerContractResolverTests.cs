using System.Text.Json;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Infrastructure
{
    public class PropertyRenameAndIgnoreSerializerContractResolverTests
    {
        [Fact]
        public void When_property_is_renamed_then_it_does_not_land_in_extension_data()
        {
            // Arrange
            var converter = new SchemaSerializationConverter();
            converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
            converter.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");

            var json = "{ \"readOnly\": true, \"nullable\": true, \"additionalProperties\": { \"nullable\": true } }";

            // Act
            var obj = JsonSchemaSerialization.FromJson<JsonSchemaProperty>(json, converter);

            // Assert
            Assert.True(obj.IsReadOnly);
            Assert.True(obj.IsNullableRaw);
            Assert.True(obj.AdditionalPropertiesSchema.IsNullableRaw);
        }

        public class MyClass
        {
            public string Foo { get; set; }
        }

        [Fact]
        public void When_property_is_renamed_then_json_is_correct()
        {
            // Arrange
            var converter = new SchemaSerializationConverter();
            converter.RenameProperty(typeof(MyClass), "Foo", "bar");

            var obj = new MyClass();
            obj.Foo = "abc";

            // Act
            var options = new JsonSerializerOptions();
            options.Converters.Add(converter);
            var json = JsonSerializer.Serialize(obj, options);
            obj = JsonSerializer.Deserialize<MyClass>(json, options);

            // Assert
            Assert.Contains("bar", json);
            Assert.Contains("abc", obj.Foo);
        }

        public class ClassWithDoubleProperties
        {
            public JsonSchema Schema { get; set; }

            public Dictionary<string, JsonSchema> Definitions1 => Definitions2;

            public Dictionary<string, JsonSchema> Definitions2 { get; set; } = new Dictionary<string, JsonSchema>();
        }

        [Fact]
        public void When_property_is_ignored_then_refs_ignore_it()
        {
            // Arrange
            var converter = new SchemaSerializationConverter();
            converter.IgnoreProperty(typeof(ClassWithDoubleProperties), "Definitions1");

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
            JsonSchemaReferenceUtilities.UpdateSchemaReferencePaths(foo, false);
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(converter);
            var json = JsonSerializer.Serialize(foo, options);

            // Assert
            Assert.Contains("#/Definitions2/Bar", json);
            Assert.DoesNotContain("#/Definitions1/Bar", json);
        }
    }
}
