using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation;

public class ValidationRegressionTests
{
    [Fact]
    public void Validate_StringFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String };

        // Act
        var errors = schema.Validate("\"hello\"");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_IntegerFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer };

        // Act
        var errors = schema.Validate("42");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NumberFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Number };

        // Act
        var errors = schema.Validate("3.14");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BooleanFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Boolean };

        // Act
        var errors = schema.Validate("true");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NullFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Null };

        // Act
        var errors = schema.Validate("null");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ArrayFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.Integer }
        };

        // Act
        var errors = schema.Validate("[1, 2, 3]");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ObjectFromJson_ReturnsNoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        // Act
        var errors = schema.Validate("{\"name\": \"test\"}");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WrongType_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String };

        // Act
        var errors = schema.Validate("42");

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_MissingRequired_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.RequiredProperties.Add("name");

        // Act
        var errors = schema.Validate("{}");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.PropertyRequired);
    }

    [Fact]
    public void Validate_MinLength_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, MinLength = 5 };

        // Act
        var errors = schema.Validate("\"ab\"");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.StringTooShort);
    }

    [Fact]
    public void Validate_Pattern_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, Pattern = "^[0-9]+$" };

        // Act
        var errors = schema.Validate("\"abc\"");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.PatternMismatch);
    }

    [Fact]
    public void Validate_Minimum_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer, Minimum = 10 };

        // Act
        var errors = schema.Validate("5");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.NumberTooSmall);
    }

    [Fact]
    public void Validate_NestedObject_ReturnsPathInErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["address"] = new JsonSchemaProperty { Type = JsonObjectType.Object };
        schema.Properties["address"].Properties["zip"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["address"].RequiredProperties.Add("zip");

        // Act
        var errors = schema.Validate("{\"address\": {}}");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Path != null && e.Path.Contains("address"));
    }

    [Fact]
    public void Validate_FormatEmail_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "email" };

        // Act
        var errors = schema.Validate("\"not-an-email\"");

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_FormatDateTime_NoErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "date-time" };

        // Act
        var errors = schema.Validate("\"2024-01-15T10:30:00Z\"");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UniqueItems_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            UniqueItems = true,
            Item = new JsonSchema { Type = JsonObjectType.Integer }
        };

        // Act
        var errors = schema.Validate("[1, 2, 1]");

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_MaxLength_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String, MaxLength = 3 };

        // Act
        var errors = schema.Validate("\"abcdef\"");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.StringTooLong);
    }

    [Fact]
    public void Validate_Maximum_ReturnsErrors()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.Integer, Maximum = 10 };

        // Act
        var errors = schema.Validate("15");

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.NumberTooBig);
    }

    [Fact]
    public void Validate_EnumValue_Valid()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String };
        schema.Enumeration.Add("red");
        schema.Enumeration.Add("green");
        schema.Enumeration.Add("blue");

        // Act
        var errors = schema.Validate("\"red\"");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EnumValue_Invalid()
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String };
        schema.Enumeration.Add("red");
        schema.Enumeration.Add("green");

        // Act
        var errors = schema.Validate("\"yellow\"");

        // Assert
        Assert.NotEmpty(errors);
    }
}
