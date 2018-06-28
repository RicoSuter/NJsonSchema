using System.Threading.Tasks;
using NJsonSchema.Annotations;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class JsonSchemaTypeAttributeTests
    {
        public class ClassPrimitiveJsonSchemaTypeAttributes
        {
            public string Foo { get; set; }

            [JsonSchemaType(typeof(int?))]
            public string Bar { get; set; }

            [JsonSchemaType(typeof(int))]
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_type_of_primitive_properties_are_changed_then_the_schemas_are_correct()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassPrimitiveJsonSchemaTypeAttributes>();
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Bar"].IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Bar"].Type.HasFlag(JsonObjectType.Integer));
            Assert.False(schema.Properties["Bar"].HasReference);

            Assert.False(schema.Properties["Baz"].IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Baz"].Type.HasFlag(JsonObjectType.Integer));
            Assert.False(schema.Properties["Baz"].HasReference);
        }

        public class ClassArrayJsonSchemaTypeAttributes
        {
            public string Foo { get; set; }

            [JsonSchemaType(typeof(string[]), IsNullable = true)]
            public string Bar { get; set; }

            [JsonSchemaType(typeof(string[]), IsNullable = false)]
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_type_of_array_properties_are_changed_then_the_schemas_are_correct()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassArrayJsonSchemaTypeAttributes>();
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Bar"].IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Bar"].Type.HasFlag(JsonObjectType.Array));
            Assert.False(schema.Properties["Bar"].HasReference);

            Assert.False(schema.Properties["Baz"].IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Baz"].Type.HasFlag(JsonObjectType.Array));
            Assert.False(schema.Properties["Baz"].HasReference);
        }

        public class ComplexClass
        {
            public string Foo { get; set; }

            public string Bar { get; set; }
        }

        public class ClassComplexJsonSchemaTypeAttributes
        {
            public string Foo { get; set; }

            [JsonSchemaType(typeof(ComplexClass), IsNullable = true)]
            public string Bar { get; set; }

            [JsonSchemaType(typeof(ComplexClass), IsNullable = false)]
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_type_of_complex_properties_are_changed_then_the_schemas_are_correct()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassComplexJsonSchemaTypeAttributes>();
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Bar"].IsNullable(SchemaType.JsonSchema));
            Assert.False(schema.Properties["Baz"].ActualTypeSchema.IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Bar"].ActualTypeSchema.Type.HasFlag(JsonObjectType.Object));

            Assert.False(schema.Properties["Baz"].IsNullable(SchemaType.JsonSchema));
            Assert.False(schema.Properties["Baz"].ActualTypeSchema.IsNullable(SchemaType.JsonSchema));
            Assert.True(schema.Properties["Baz"].ActualTypeSchema.Type.HasFlag(JsonObjectType.Object));
            Assert.True(schema.Properties["Baz"].HasReference);
        }

        [JsonSchemaType(typeof(ComplexClass))]
        public class ComplexClassWithJsonSchemaTypeAttribute
        {
            public string Foo { get; set; }
        }

        public class ClassWithReference
        {
            public ComplexClassWithJsonSchemaTypeAttribute Abc { get; set; }

            [NotNull]
            public ComplexClassWithJsonSchemaTypeAttribute Def { get; set; }
        }

        [Fact]
        public async Task When_JsonSchemaTypeAttribute_is_on_class_then_property_defines_nullability()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithReference>();
            var json = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Abc"].IsNullable(SchemaType.JsonSchema));
            Assert.False(schema.Properties["Def"].IsNullable(SchemaType.JsonSchema));

            var reference = schema.Properties["Abc"].ActualTypeSchema;
            Assert.Equal(2, reference.Properties.Count);
            Assert.True(reference.Properties.ContainsKey("Foo"));
            Assert.True(reference.Properties.ContainsKey("Bar"));
        }
    }
}
