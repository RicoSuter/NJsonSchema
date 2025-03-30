using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class ExtensionDataGenerationTests
    {
        public class ClassWithExtensionData
        {
            public string Foo { get; set; }

            [JsonExtensionData]
            public IDictionary<string, object> ExtensionData { get; set; }
        }

        [Fact]
        public async Task When_class_has_property_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithExtensionData>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            // Assert
            Assert.Single(schema.ActualProperties);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }
    }
}
