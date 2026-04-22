using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation;

public class SampleJsonDataGeneratorRegressionTests
{
    [Fact]
    public void Generate_StringProperty_ReturnsString()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        Assert.IsType<JsonObject>(token);
        var obj = (JsonObject)token!;
        Assert.NotNull(obj["name"]);
        Assert.Equal(JsonValueKind.String, obj["name"]!.GetValueKind());
    }

    [Fact]
    public void Generate_IntegerProperty_ReturnsInteger()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["count"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        Assert.Equal(JsonValueKind.Number, obj["count"]!.GetValueKind());
    }

    [Fact]
    public void Generate_NumberProperty_ReturnsNumber()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["value"] = new JsonSchemaProperty { Type = JsonObjectType.Number };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        var val = obj["value"];
        Assert.NotNull(val);
        Assert.Equal(JsonValueKind.Number, val!.GetValueKind());
    }

    [Fact]
    public void Generate_BooleanProperty_ReturnsBoolean()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["active"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        var kind = obj["active"]!.GetValueKind();
        Assert.True(kind == JsonValueKind.True || kind == JsonValueKind.False);
    }

    [Fact]
    public void Generate_ArrayProperty_ReturnsArray()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["items"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        Assert.IsType<JsonArray>(obj["items"]);
    }

    [Fact]
    public void Generate_NestedObject_ReturnsNestedObject()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["address"] = new JsonSchemaProperty { Type = JsonObjectType.Object };
        schema.Properties["address"].Properties["city"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        var address = obj["address"];
        Assert.NotNull(address);
        Assert.IsType<JsonObject>(address);
        Assert.NotNull(((JsonObject)address!)["city"]);
    }

    [Fact]
    public void Generate_EnumProperty_ReturnsEnumValue()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        var prop = new JsonSchemaProperty { Type = JsonObjectType.String };
        prop.Enumeration.Add("red");
        prop.Enumeration.Add("green");
        prop.Enumeration.Add("blue");
        schema.Properties["color"] = prop;
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        var color = obj["color"]?.GetValue<string>();
        Assert.Contains(color, new[] { "red", "green", "blue" });
    }

    [Fact]
    public void Generate_WithDefaultValue_UsesDefault()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["status"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Default = "active"
        };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);

        // Assert
        var obj = (JsonObject)token!;
        Assert.Equal("active", obj["status"]?.GetValue<string>());
    }

    [Fact]
    public void Generate_OutputIsValidJson()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["age"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        schema.Properties["active"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean };
        var generator = new SampleJsonDataGenerator();

        // Act
        var token = generator.Generate(schema);
        var jsonString = token!.ToJsonString();

        // Assert
        Assert.NotEmpty(jsonString);
        var reparsed = JsonNode.Parse(jsonString);
        Assert.NotNull(reparsed);
    }
}
