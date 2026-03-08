using Newtonsoft.Json;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization;

public class SchemaSerializationRoundTripTests
{
    [Fact]
    public async Task RoundTrip_SimpleSchema()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Title = "Test",
            Description = "A test schema"
        };
        schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            MinLength = 1,
            MaxLength = 100
        };
        schema.Properties["age"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Integer,
            Minimum = 0,
            Maximum = 150
        };
        schema.RequiredProperties.Add("name");

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Equal("Test", deserialized.Title);
        Assert.Equal(JsonObjectType.String, deserialized.Properties["name"].Type);
        Assert.Contains("name", deserialized.RequiredProperties);
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.OpenApi3)]
    [InlineData(SchemaType.Swagger2)]
    public void RoundTrip_PerSchemaType(SchemaType schemaType)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["value"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };

        // Act
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(schemaType);
        var json = JsonSchemaSerialization.ToJson(schema, schemaType, resolver, Formatting.Indented);
        var deserialized = JsonSchemaSerialization.FromJson<JsonSchema>(json, resolver);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(JsonObjectType.Object, deserialized!.Type);
        Assert.True(deserialized.Properties.ContainsKey("value"));
    }

    [Fact]
    public async Task RoundTrip_SchemaWithAllOf()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        var allOfSchema = new JsonSchema { Type = JsonObjectType.Object };
        allOfSchema.Properties["id"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        schema.AllOf.Add(allOfSchema);

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Single(deserialized.AllOf);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithOneOfAndAnyOf()
    {
        // Arrange
        var schema = new JsonSchema();
        schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.String });
        schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.Integer });
        schema.AnyOf.Add(new JsonSchema { Type = JsonObjectType.Number });

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Equal(2, deserialized.OneOf.Count);
        Assert.Single(deserialized.AnyOf);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithEnumeration()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String };
        schema.Enumeration.Add("red");
        schema.Enumeration.Add("green");
        schema.Enumeration.Add("blue");
        schema.EnumerationNames.Add("Red");
        schema.EnumerationNames.Add("Green");
        schema.EnumerationNames.Add("Blue");

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Equal(3, deserialized.Enumeration.Count);
        Assert.Equal(3, deserialized.EnumerationNames.Count);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithDefinitionsAndReferences()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        var addressSchema = new JsonSchema { Type = JsonObjectType.Object };
        addressSchema.Properties["street"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Definitions["Address"] = addressSchema;
        schema.Properties["address"] = new JsonSchemaProperty();
        schema.Properties["address"].Reference = addressSchema;

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.NotNull(deserialized.Properties["address"].Reference);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithExclusiveMinMax()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Number,
            ExclusiveMinimum = 0,
            ExclusiveMaximum = 100
        };

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.Equal(0, deserialized.ExclusiveMinimum);
        Assert.Equal(100, deserialized.ExclusiveMaximum);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithAdditionalPropertiesFalse()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = false
        };

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.False(deserialized.AllowAdditionalProperties);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithPatternProperties()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.PatternProperties["^x-"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Single(deserialized.PatternProperties);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithItems()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Equal(JsonObjectType.String, deserialized.Item.Type);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithMultipleTypes()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.String | JsonObjectType.Null
        };

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.True(deserialized.Type.HasFlag(JsonObjectType.String));
        Assert.True(deserialized.Type.HasFlag(JsonObjectType.Null));
    }
}
