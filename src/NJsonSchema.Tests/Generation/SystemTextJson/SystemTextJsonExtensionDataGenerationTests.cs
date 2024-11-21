using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonExtensionDataGenerationTests
    {
        public class ClassWithObjectExtensionData
        {
            public string Foo { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }
        }
        
        public class ClassWithJsonElementExtensionData
        {
            public string Foo { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public class ClassWithIndexedProperty
        {
            public double X { get; set; }
            public double Y { get; set; }

            public double this[int indexer]
            {
                get
                {
                    switch (indexer)
                    {
                        case 0: return X;
                        case 1: return Y;
                        default: throw new ArgumentOutOfRangeException(nameof(indexer));
                    }
                }
                set
                {
                    switch (indexer)
                    {
                        case 0: X = value; break;
                        case 1: Y = value; break;
                        default: throw new ArgumentOutOfRangeException(nameof(indexer));
                    }
                }
            }

            public double this[string indexer]
            {
                get
                {
                    switch (indexer)
                    {
                        case "X": return X;
                        case "Y": return Y;
                        default: throw new ArgumentOutOfRangeException(nameof(indexer));
                    }
                }
                set
                {
                    switch (indexer)
                    {
                        case "X": X = value; break;
                        case "Y": Y = value; break;
                        default: throw new ArgumentOutOfRangeException(nameof(indexer));
                    }
                }
            }
        }

        [Fact]
        public void SystemTextJson_When_class_has_object_Dictionary_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set()
        {
            //// Act
            var schema = JsonSchemaGenerator.FromType<ClassWithObjectExtensionData>(new SystemTextJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }
        
        [Fact]
        public void SystemTextJson_When_class_has_JsonElement_Dictionary_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set()
        {
            //// Act
            var schema = JsonSchemaGenerator.FromType<ClassWithJsonElementExtensionData>(new SystemTextJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3
            });

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }

        [Fact]
        public void SystemTextJson_When_class_has_Indexed_properties_then_Generates_schema_without_them()
        {
            // Act
            var schema = JsonSchemaGenerator.FromType<ClassWithIndexedProperty>(new SystemTextJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.JsonSchema
            });

            // Assert
            Assert.Equal(2, schema.ActualProperties.Count);
            Assert.All(schema.ActualProperties, property => Assert.False(string.Equals(property.Key, "Item", StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}