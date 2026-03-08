using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class SchemaSerializationSnapshotTests
{
    private static JsonSchema CreateComplexSchema()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Title = "Person",
            Description = "A person object"
        };

        schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            MinLength = 1,
            MaxLength = 200,
            Pattern = "^[a-zA-Z ]+$"
        };

        schema.Properties["age"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Integer,
            Minimum = 0,
            Maximum = 150
        };

        schema.Properties["email"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Format = "email"
        };

        schema.RequiredProperties.Add("name");
        schema.RequiredProperties.Add("age");

        var addressSchema = new JsonSchema { Type = JsonObjectType.Object };
        addressSchema.Properties["street"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        addressSchema.Properties["city"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Definitions["Address"] = addressSchema;

        schema.Properties["address"] = new JsonSchemaProperty();
        schema.Properties["address"].Reference = addressSchema;

        schema.Properties["tags"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };

        return schema;
    }

    [Fact]
    public async Task Snapshot_ComplexSchema_JsonSchemaType()
    {
        // Arrange
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.JsonSchema);

        // Act
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.JsonSchema, resolver, Formatting.Indented);

        // Assert
        await VerifyHelper.Verify(json);
    }

    [Fact]
    public async Task Snapshot_ComplexSchema_OpenApi3()
    {
        // Arrange
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.OpenApi3);

        // Act
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, Formatting.Indented);

        // Assert
        await VerifyHelper.Verify(json);
    }

    [Fact]
    public async Task Snapshot_ComplexSchema_Swagger2()
    {
        // Arrange
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.Swagger2);

        // Act
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, Formatting.Indented);

        // Assert
        await VerifyHelper.Verify(json);
    }
}
