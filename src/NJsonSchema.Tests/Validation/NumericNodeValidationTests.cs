using NJsonSchema.Validation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace NJsonSchema.Tests.Validation;

public class NumericNodeValidationTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("1e0")]
    [InlineData("100e-2")]
    [InlineData("-0")]
    [InlineData("0.000e999999")]
    [InlineData("9007199254740993")]
    [InlineData("1e1000")]
    [InlineData("1e999999999999999999999999999999999999")]
    public void Integer_WithWholeExactValue_IsAccepted(string json)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };

        // Act
        var errors = schema.Validate(json);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("1.00000000000000001")]
    [InlineData("1e-1000")]
    [InlineData("-0.5")]
    [InlineData("-1.00000000000000001")]
    [InlineData("1e-999999999999999999999999999999999999")]
    public void Integer_WithNonIntegralExactValue_IsRejected(string json)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };

        // Act
        var errors = schema.Validate(json);

        // Assert
        Assert.Contains(errors, error => error.Kind == ValidationErrorKind.IntegerExpected);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Integer_WithNonFiniteClrValue_Throws(double number)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };
        var node = JsonValue.Create(number);

        // Act
        var exception = Record.Exception(() => new JsonSchemaValidator().Validate(node, schema));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Integer_WithMalformedJsonNumber_Throws()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };

        // Act
        var exception = Record.Exception(() => schema.Validate("1e+"));

        // Assert
        Assert.IsAssignableFrom<JsonException>(exception);
    }
}
