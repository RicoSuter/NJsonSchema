using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class DictionaryTests
    {
        public enum PropertyName
        {
            Name,
            Gender
        }

        public class EnumKeyDictionaryTest
        {
            public Dictionary<PropertyName, string> Mapping { get; set; }

            public IDictionary<PropertyName, string> Mapping2 { get; set; }

            public IDictionary<PropertyName, int?> Mapping3 { get; set; }

            public IDictionary<PropertyName, double?> Mapping4 { get; set; }
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_csharp_has_enum_key()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Mapping"].IsDictionary);
            Assert.True(schema.Properties["Mapping"].DictionaryKey.ActualSchema.IsEnumeration);

            Assert.True(schema.Properties["Mapping2"].IsDictionary);
            Assert.True(schema.Properties["Mapping2"].DictionaryKey.ActualSchema.IsEnumeration);

            Assert.False(schema.Properties["Mapping2"].DictionaryKey.IsNullable(SchemaType.JsonSchema));
            Assert.False(schema.Properties["Mapping2"].AdditionalPropertiesSchema.IsNullable(SchemaType.JsonSchema));
        }

        [Fact]
        public async Task When_value_type_is_nullable_then_json_schema_is_nullable()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Mapping3"].IsDictionary);
            Assert.True(schema.Properties["Mapping3"].AdditionalPropertiesSchema.IsNullable(SchemaType.JsonSchema));

            Assert.True(schema.Properties["Mapping3"].IsDictionary);
            Assert.True(schema.Properties["Mapping4"].AdditionalPropertiesSchema.IsNullable(SchemaType.JsonSchema));
        }

        [Fact]
        public async Task When_value_type_is_nullable_then_json_schema_is_nullable_Swagger2()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2,
                GenerateCustomNullableProperties = true
            });
            var data = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Mapping3"].IsDictionary);
            Assert.True(schema.Properties["Mapping3"].AdditionalPropertiesSchema.IsNullable(SchemaType.Swagger2));

            Assert.True(schema.Properties["Mapping4"].IsDictionary);
            Assert.True(schema.Properties["Mapping4"].AdditionalPropertiesSchema.IsNullable(SchemaType.Swagger2));
        }

        [Fact]
        public async Task When_value_type_is_nullable_then_json_schema_is_nullable_OpenApi3()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3,
                GenerateCustomNullableProperties = true
            });
            var data = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Mapping3"].IsDictionary);
            Assert.True(schema.Properties["Mapping3"].AdditionalPropertiesSchema.IsNullable(SchemaType.OpenApi3));

            Assert.True(schema.Properties["Mapping4"].IsDictionary);
            Assert.True(schema.Properties["Mapping4"].AdditionalPropertiesSchema.IsNullable(SchemaType.OpenApi3));
        }
    }
}