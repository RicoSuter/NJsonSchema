using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class SchemaTypePropertyBehaviorTests
{
    [Fact]
    public void Swagger2_AdditionalProperties_EmptyObjectWhenAllowed()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(SchemaType.Swagger2);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, true);

        // Assert
        Assert.Contains("\"additionalProperties\": {}", json);
    }

    [Fact]
    public void JsonSchemaType_AdditionalProperties_OmittedWhenAllowed()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(SchemaType.JsonSchema);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.JsonSchema, resolver, true);

        // Assert
        Assert.DoesNotContain("additionalProperties", json);
    }

    [Fact]
    public void OpenApi3_AdditionalProperties_OmittedWhenAllowed()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, true);

        // Assert
        Assert.DoesNotContain("additionalProperties", json);
    }

    [Fact]
    public void EmptyCollections_AreNotSerialized()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.String
        };

        // Act
        var json = schema.ToJson();

        // Assert
        Assert.DoesNotContain("\"properties\"", json);
        Assert.DoesNotContain("\"allOf\"", json);
        Assert.DoesNotContain("\"oneOf\"", json);
        Assert.DoesNotContain("\"anyOf\"", json);
        Assert.DoesNotContain("\"definitions\"", json);
        Assert.DoesNotContain("\"required\"", json);
        Assert.DoesNotContain("\"enum\"", json);
    }

    [Fact]
    public async Task ExtensionData_SurvivesRoundTrip()
    {
        // Arrange
        var json = @"{
            ""type"": ""object"",
            ""x-custom-tag"": ""hello"",
            ""x-custom-number"": 42
        }";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);
        var output = schema.ToJson();

        // Assert
        Assert.Contains("x-custom-tag", output);
        Assert.Contains("x-custom-number", output);
    }

    [Fact]
    public async Task ExtensionData_SchemasAreDeserialized()
    {
        // Arrange
        var json = @"{
            ""type"": ""object"",
            ""x-schema"": {
                ""type"": ""string"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                }
            }
        }";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.NotNull(schema.ExtensionData);
        Assert.True(schema.ExtensionData!.ContainsKey("x-schema"));
        Assert.IsType<JsonSchema>(schema.ExtensionData["x-schema"]);
    }

    [Fact]
    public void Discriminator_Swagger2_SerializedAsString()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Discriminator = "type";

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(SchemaType.Swagger2);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, true);

        // Assert
        Assert.Contains("\"discriminator\": \"type\"", json);
    }

    [Fact]
    public void Discriminator_OpenApi3_SerializedAsObject()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.DiscriminatorObject = new OpenApiDiscriminator
        {
            PropertyName = "type"
        };

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, true);

        // Assert
        Assert.Contains("\"propertyName\"", json);
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.OpenApi3)]
    [InlineData(SchemaType.Swagger2)]
    public void AdditionalPropertiesFalse_SerializedCorrectly(SchemaType schemaType)
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = false
        };

        // Act
        var resolver = JsonSchema.CreateSchemaSerializationConverter(schemaType);
        var json = JsonSchemaSerialization.ToJson(schema, schemaType, resolver, true);

        // Assert
        if (schemaType == SchemaType.Swagger2)
        {
            Assert.DoesNotContain("additionalProperties", json);
        }
        else
        {
            Assert.Contains("\"additionalProperties\": false", json);
        }
    }
}
