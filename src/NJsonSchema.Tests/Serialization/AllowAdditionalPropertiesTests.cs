using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Serialization
{
    public class AllowAdditionalPropertiesTests
    {
        [Theory]
        [InlineData(SchemaType.JsonSchema, "{}", true)]
        [InlineData(SchemaType.OpenApi3, "{}", true)]
        [InlineData(SchemaType.Swagger2, "{}", false)]
        [InlineData(SchemaType.JsonSchema, "{\"additionalProperties\":false}", false)]
        [InlineData(SchemaType.JsonSchema, "{\"additionalProperties\":true}", true)]
        [InlineData(SchemaType.JsonSchema, "{\"additionalProperties\":{}}", true)]
        [InlineData(SchemaType.OpenApi3, "{\"additionalProperties\":false}", false)]
        [InlineData(SchemaType.OpenApi3, "{\"additionalProperties\":true}", true)]
        [InlineData(SchemaType.OpenApi3, "{\"additionalProperties\":{}}", true)]
        [InlineData(SchemaType.Swagger2, "{\"additionalProperties\":{}}", true)]
        public async Task When_schema_JSON_is_deserialized_then_AllowAdditionalProperties_is_correct(SchemaType schemaType, string json, bool expectedAllowAdditionalProperties)
        {
            // Act
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, schemaType, null, factory, new DefaultContractResolver());

            // Assert
            Assert.Equal(expectedAllowAdditionalProperties, schema.AllowAdditionalProperties);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema, "{}")]
        [InlineData(SchemaType.OpenApi3, "{}")]
        [InlineData(SchemaType.Swagger2, "{\"additionalProperties\":{}}")]
        public void When_default_schema_is_serialized_then_AllowAdditionalProperties_is_correct(SchemaType schemaType, string expectedJson)
        {
            // default schema (new JsonSchema) has always AllowAdditionalProperties = true

            // Act
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var json = JsonSchemaSerialization.ToJson(new JsonSchema(), schemaType, new DefaultContractResolver(), Formatting.None);

            // Assert
            Assert.Equal(expectedJson, json);
        }

        [Theory]
        [InlineData(SchemaType.JsonSchema, true, "{}")]
        [InlineData(SchemaType.JsonSchema, false, "{\"additionalProperties\":false}")]
        [InlineData(SchemaType.OpenApi3, true, "{}")]
        [InlineData(SchemaType.OpenApi3, false, "{\"additionalProperties\":false}")]
        [InlineData(SchemaType.Swagger2, true, "{\"additionalProperties\":{}}")]
        [InlineData(SchemaType.Swagger2, false, "{}")]
        public void When_AllowAdditionalProperties_is_true_then_JSON_is_correct(SchemaType schemaType, bool allowAdditionalProperties, string expectedJson)
        {
            // Act
            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var json = JsonSchemaSerialization.ToJson(new JsonSchema { AllowAdditionalProperties = allowAdditionalProperties }, schemaType, new DefaultContractResolver(), Formatting.None);

            // Assert
            Assert.Equal(expectedJson, json);
        }
    }
}
