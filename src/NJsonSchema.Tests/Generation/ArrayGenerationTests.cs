using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class ArrayGenerationTests
    {
        public class ClassWithJArray
        {
            public string Foo { get; set; }

            public JArray Array { get; set; }
        }

        [Fact]
        public async Task When_property_is_JArray_then_schema_with_any_array_is_generated()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithJArray>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(2, schema.ActualProperties.Count);
            var arrayProperty = schema.ActualProperties["Array"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, arrayProperty.Type);
            Assert.True(arrayProperty.Item.ActualTypeSchema.IsAnyType);
        }
    }
}