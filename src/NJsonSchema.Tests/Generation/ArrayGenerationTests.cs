using System.Collections.Generic;
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
            var schema = JsonSchema.FromType<ClassWithJArray>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(2, schema.ActualProperties.Count);
            var arrayProperty = schema.ActualProperties["Array"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, arrayProperty.Type);
            Assert.True(arrayProperty.Item.ActualTypeSchema.IsAnyType);
        }
#nullable enable
        public class ClassWithArrayOfNullable
        {
            public string?[] Array { get; set; } = new string?[0];
        }
#nullable restore

        [Fact]
        public async Task When_property_is_Array_of_nullable_then_schema_with_array_of_nullable_is_generated()
        {
            //// Act
            var schema = JsonSchema.FromType<ClassWithArrayOfNullable>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(1, schema.ActualProperties.Count);
            var arrayProperty = schema.ActualProperties["Array"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, arrayProperty.Type);
            Assert.True(arrayProperty.Item.IsNullableRaw);
        }

        public class ListContainer
        {
            public SomeModelCollectionResponse Response { get; set; }
        }

        public class SomeModel
        {
            public string Foo { get; set; }
        }

        public class SomeModelCollectionResponse : List<SomeModel> { }

        [Fact]
        public async Task When_class_inherits_from_list_then_schema_is_inlined_and_type_is_array()
        {
            //// Act
            var schema = JsonSchema.FromType<ListContainer>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Array, schema.Definitions["SomeModelCollectionResponse"].Type);
            Assert.NotNull(schema.Definitions["SomeModelCollectionResponse"].Item);
        }
    }
}