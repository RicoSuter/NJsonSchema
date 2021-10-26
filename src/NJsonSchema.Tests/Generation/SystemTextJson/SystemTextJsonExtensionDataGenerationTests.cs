#if !NET461

using System.Collections.Generic;
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

        [Fact]
        public void SystemTextJson_When_class_has_object_Dictionary_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set()
        {
            //// Act
            var schema = JsonSchema.FromType<ClassWithObjectExtensionData>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3,
                SerializerOptions = new JsonSerializerOptions()
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
            var schema = JsonSchema.FromType<ClassWithJsonElementExtensionData>(new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3,
                SerializerOptions = new JsonSerializerOptions()
            });

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }
    }
}

#endif