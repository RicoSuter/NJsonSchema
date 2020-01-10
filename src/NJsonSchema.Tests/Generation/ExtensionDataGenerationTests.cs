using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Annotations;
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
            var schema = JsonSchema.FromType<ClassWithExtensionData>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.AllowAdditionalProperties);
            Assert.True(schema.AdditionalPropertiesSchema.ActualSchema.IsAnyType);
        }

        public class SubData2
        {
            public int id { get; set; }
        }

        public class ClassWithExtensionData2
        {
            [JsonSchemaExtensionDataAttribute("x-other", "barfoo")]
            public string other { get; set; }

            [JsonSchemaExtensionDataAttribute("x-data", "foobar")]
            public SubData2 data { get; set; }
        }

        public class ClassWithoutExtensionData2
        {
            public string other { get; set; }

            public SubData2 noAttribute { get; set; }
        }

        public class Wrapper2
        {
            public ClassWithoutExtensionData2 noExtensionData { get; set; }

            public ClassWithExtensionData2 hasExtensionData { get; set; }
        }

        [Fact]
        public async Task When_class_has_property_with_JsonExtensionDataAttribute_on_property_then_AdditionalProperties_schema_is_set_with_values()
        {
            //// Act
            var schema = JsonSchema.FromType<Wrapper2>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();
        }
    }
}
