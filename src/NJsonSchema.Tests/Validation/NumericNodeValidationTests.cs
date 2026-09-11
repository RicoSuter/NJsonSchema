using NJsonSchema.Validation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace NJsonSchema.Tests.Validation;

public class NumericNodeValidationTests
{
    public static IEnumerable<object[]> NumericNodes()
    {
        foreach (var number in new object[] { (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1U, 1L, 1UL, 1F, 1D, 1M,
            (sbyte)-1, (short)-1, -1, -1L, -1.5F, -1.5D, -1.5M, ulong.MaxValue, long.MaxValue, float.MaxValue, double.MaxValue, 16777218F, 9007199254740991D })
        {
            yield return new[] { number };
        }
    }

    [Theory]
    [MemberData(nameof(NumericNodes))]
    public void DirectNumericNode_MatchesParsedConstraints(object number)
    {
        // Arrange
        var node = number switch
        {
            sbyte value => JsonValue.Create(value)!,
            byte value => JsonValue.Create(value)!,
            short value => JsonValue.Create(value)!,
            ushort value => JsonValue.Create(value)!,
            int value => JsonValue.Create(value)!,
            uint value => JsonValue.Create(value)!,
            long value => JsonValue.Create(value)!,
            ulong value => JsonValue.Create(value)!,
            float value => JsonValue.Create(value)!,
            double value => JsonValue.Create(value)!,
            decimal value => JsonValue.Create(value)!,
            _ => throw new ArgumentException(nameof(number))
        };
        var validator = new JsonSchemaValidator();
        var schemas = new[]
        {
            new JsonSchema { Type = JsonObjectType.Integer },
            new JsonSchema { Type = JsonObjectType.Number },
            new JsonSchema { Minimum = 5 },
            new JsonSchema { Maximum = 0 },
            new JsonSchema { Minimum = 1, IsExclusiveMinimum = true },
            new JsonSchema { Maximum = 1, IsExclusiveMaximum = true },
            new JsonSchema { ExclusiveMinimum = 1, ExclusiveMaximum = 1 },
            new JsonSchema { MultipleOf = 2 },
            new JsonSchema { Maximum = long.MaxValue },
            new JsonSchema { Minimum = 16777219 },
            new JsonSchema { Maximum = 9007199254740990M },
            new JsonSchema { MultipleOf = 3 }
        };

        // Act / Assert
        foreach (var schema in schemas)
        {
            var expected = validator.Validate(node.ToJsonString(), schema).Select(error => error.Kind);
            var actual = validator.Validate(node, schema).Select(error => error.Kind);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void GeneratedNumericNode_EnforcesMinimum()
    {
        // Arrange
        var node = new NJsonSchema.Generation.SampleJsonDataGenerator().Generate(new JsonSchema { Type = JsonObjectType.Integer });
        var schema = new JsonSchema { Minimum = 100 };

        // Act
        var errors = new JsonSchemaValidator().Validate(node, schema);

        // Assert
        Assert.Contains(errors, error => error.Kind == ValidationErrorKind.NumberTooSmall);
    }

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
