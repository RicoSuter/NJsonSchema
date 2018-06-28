using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Generation;
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
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithExtensionData>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }
    }
}
