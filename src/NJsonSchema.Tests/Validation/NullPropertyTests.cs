using NJsonSchema.NewtonsoftJson.Generation;
using System;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class NullPropertyTests
    {
        [Fact]
        public void When_property_can_be_null_then_null_is_allowed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Properties["test"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Null | JsonObjectType.Object
            };
            schema.Properties["test"].Properties["foo"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.String
            };

            //// Act
            var data = "{ 'test': null }";
            var errors = schema.Validate(data);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        public class NullablePropertyClass<T>
        {
            public T Value { get; set; }
        }

        public class QueryRule
        {
            public string Name { get; set; }
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_object_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<QueryRule>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_number_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<int?>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_string_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<string>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_boolean_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<bool?>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_array_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<int[]>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema)]
        [InlineData(SchemaType.OpenApi3)]
        [InlineData(SchemaType.Swagger2)]
        public async Task When_enum_property_can_be_null_then_null_is_allowed(SchemaType schemaType)
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NullablePropertyClass<AttributeTargets?>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = schemaType
            });

            //// Act
            var data = "{ 'Value': null }";
            var errors = schema.Validate(data, schemaType);

            //// Assert
            Assert.Equal(0, errors.Count);
        }
    }
}