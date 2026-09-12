#nullable enable
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.Tests.Generation;

public class NewtonsoftTokenContractTests
{
    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Tokens_are_free_form_without_reflected_members(SchemaType dialect)
    {
        // Arrange
        var settings = new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = dialect };

        // Act
        foreach (var type in new[] { typeof(JObject), typeof(JToken), typeof(JsonObject), typeof(JsonNode) })
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType(type, settings);
            var wire = JsonNode.Parse(schema.ToJson())!.AsObject();

            // Assert
            Assert.Null(schema.Title);
            Assert.Equal(JsonObjectType.None, schema.ActualSchema.Type);
            Assert.Empty(schema.Properties);
            Assert.Empty(schema.Definitions);
            Assert.Equal(dialect != SchemaType.Swagger2, schema.AllowAdditionalProperties);
            Assert.Null(wire["type"]);
            Assert.Null(wire["title"]);
            Assert.Null(wire["properties"]);
            Assert.Null(wire["definitions"]);
            if (dialect == SchemaType.Swagger2) Assert.False(wire["additionalProperties"]!.GetValue<bool>());
            else Assert.Null(wire["additionalProperties"]);
            var dialectWire = JsonNode.Parse(JsonSchemaSerialization.ToJson(schema, dialect, JsonSchema.CreateSchemaSerializationConverter(dialect), false))!;
            Assert.Null(dialectWire["additionalProperties"]);
        }

        var holder = NewtonsoftJsonSchemaGenerator.FromType<TokenHolder>(settings);
        Assert.Empty(holder.Definitions);
        foreach (var property in holder.Properties.Values)
        {
            Assert.Null(property.ActualSchema.Title);
            Assert.Empty(property.ActualSchema.Properties);
            Assert.False(property.ActualSchema.Type.HasFlag(JsonObjectType.Array));
            Assert.Equal(dialect != SchemaType.Swagger2, property.ActualSchema.AllowAdditionalProperties);
        }
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Arrays_and_explicit_type_mappers_keep_precedence(SchemaType dialect)
    {
        // Arrange
        var settings = new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = dialect };

        // Act
        var arrays = new[] { typeof(JArray), typeof(JsonArray) }.Select(type => NewtonsoftJsonSchemaGenerator.FromType(type, settings)).ToArray();
        settings.TypeMappers.Add(new PrimitiveTypeMapper(typeof(JObject), schema => schema.Type = JsonObjectType.String));
        var mapped = NewtonsoftJsonSchemaGenerator.FromType<JObject>(settings);
        var holder = NewtonsoftJsonSchemaGenerator.FromType<TokenHolder>(settings);

        // Assert
        Assert.All(arrays, schema => Assert.Equal(JsonObjectType.Array, schema.Type));
        Assert.Equal(JsonObjectType.String, mapped.Type);
        Assert.True(holder.Properties["Object"].ActualTypeSchema.Type.HasFlag(JsonObjectType.String));
    }

    public class TokenHolder
    {
        public JObject Object { get; set; } = new();
        public JToken Token { get; set; } = new JObject();
    }
}
