using Newtonsoft.Json.Linq;
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
        Assert.IsType<JObject>(token);
        Assert.NotNull(((JObject)token)["name"]);
        Assert.Equal(JTokenType.String, ((JObject)token)["name"]!.Type);
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
        Assert.Equal(JTokenType.Integer, ((JObject)token)["count"]!.Type);
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
        var val = ((JObject)token)["value"];
        Assert.NotNull(val);
        Assert.True(val!.Type == JTokenType.Float || val.Type == JTokenType.Integer);
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
        Assert.Equal(JTokenType.Boolean, ((JObject)token)["active"]!.Type);
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
        Assert.Equal(JTokenType.Array, ((JObject)token)["items"]!.Type);
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
        var address = ((JObject)token)["address"];
        Assert.NotNull(address);
        Assert.Equal(JTokenType.Object, address!.Type);
        Assert.NotNull(((JObject)address)["city"]);
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
        var color = ((JObject)token)["color"]?.Value<string>();
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
        Assert.Equal("active", ((JObject)token)["status"]?.Value<string>());
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
        var jsonString = token.ToString();

        // Assert
        Assert.NotEmpty(jsonString);
        var reparsed = JToken.Parse(jsonString);
        Assert.NotNull(reparsed);
    }
}
