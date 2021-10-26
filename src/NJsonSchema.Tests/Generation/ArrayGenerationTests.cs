using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
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
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithJArray>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
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

            public List<string?> List { get; set; } = new List<string?>();
        }
#nullable restore

        [Fact]
        public async Task When_property_is_Array_of_nullable_then_schema_with_array_of_nullable_is_generated()
        {
            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithArrayOfNullable>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(2, schema.ActualProperties.Count);

            var arrayProperty = schema.ActualProperties["Array"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, arrayProperty.Type);
            Assert.True(arrayProperty.Item.IsNullableRaw);

            var listProperty = schema.ActualProperties["List"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, listProperty.Type);
            Assert.True(listProperty.Item.IsNullableRaw);
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
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ListContainer>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Array, schema.Definitions["SomeModelCollectionResponse"].Type);
            Assert.NotNull(schema.Definitions["SomeModelCollectionResponse"].Item);
        }

#if NET5_0

        public class ClassWithAsyncEnumerable
        {
            public IAsyncEnumerable<Apple> AsyncApples { get; set; }

            public List<Apple> AppleList { get; set; }
        }

        public class Apple
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task When_property_is_async_numerable_then_item_type_is_correct()
        {
            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithAsyncEnumerable>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            //// Assert
            var asyncProperty = schema.ActualProperties["AsyncApples"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, asyncProperty.Type);
            Assert.True(asyncProperty.Item.HasReference);

            var listProperty = schema.ActualProperties["AppleList"].ActualTypeSchema;
            Assert.Equal(JsonObjectType.Array, listProperty.Type);
            Assert.True(listProperty.Item.HasReference);
        }

#endif
    }
}