using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema.Infrastructure;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation;

public class JsonValueValidationTests
{
    public static IEnumerable<object[]> EnumCases()
    {
        foreach (var schemaType in new[] { SchemaType.JsonSchema, SchemaType.Swagger2, SchemaType.OpenApi3 })
        {
            foreach (var values in new[]
            {
                new[] { "true", "true", "false" },
                new[] { "false", "false", "true" },
                new[] { "null", "null", "0" },
                new[] { "1.0", "1", "\"1\"" },
                new[] { "\"1\"", "\"1\"", "1" },
                new[] { "9007199254740993", "9007199254740993", "9007199254740992" },
                new[] { "1.00000000000000001", "100000000000000001e-17", "1" },
                new[] { "{\"a\":1,\"b\":[true,null]}", "{\"b\":[true,null],\"a\":1.0}", "{\"a\":1}" },
                new[] { "[1,true,null]", "[1.0,true,null]", "[true,1,null]" }
            })
            {
                yield return new object[] { schemaType, values[0], values[1], true };
                yield return new object[] { schemaType, values[0], values[2], false };
            }
        }
    }

    [Theory]
    [MemberData(nameof(EnumCases))]
    public async Task Enum_ComparesJsonValues(SchemaType schemaType, string enumeration, string instance, bool matches)
    {
        // Arrange
        var converter = JsonSchema.CreateSchemaSerializationConverter(schemaType);
        var schema = await JsonSchemaSerialization.FromJsonAsync<JsonSchema>("{\"enum\":[" + enumeration + "]}", schemaType, null,
            JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator()), converter);

        // Act
        var errors = new JsonSchemaValidator().Validate(instance, schema, schemaType);

        // Assert
        Assert.Equal(!matches, errors.Any(error => error.Kind == ValidationErrorKind.NotInEnumeration));
    }

    [Theory]
    [InlineData("[9007199254740992,9007199254740993]", true)]
    [InlineData("[1,1.0,100e-2]", false)]
    [InlineData("[-0,0.000e999999]", false)]
    [InlineData("[1e100000000000000000000,10e99999999999999999999]", false)]
    [InlineData("[1.00000000000000001,1]", true)]
    [InlineData("[{\"a\":1,\"b\":{\"x\":true,\"y\":null}},{\"b\":{\"y\":null,\"x\":true},\"a\":1.0}]", false)]
    [InlineData("[[1,2],[2,1]]", true)]
    [InlineData("[[1,2],[1.0,2e0]]", false)]
    [InlineData("[{}, {\"a\":null}]", true)]
    [InlineData("[\"1\",1]", true)]
    [InlineData("[true,\"true\"]", true)]
    [InlineData("[{\"a\":1},{\"A\":1}]", true)]
    public async Task UniqueItems_ComparesExactStructures(string instance, bool unique)
    {
        // Arrange
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"array\",\"uniqueItems\":true}");

        // Act
        var errors = schema.Validate(instance);

        // Assert
        Assert.Equal(!unique, errors.Any(error => error.Kind == ValidationErrorKind.ItemsNotUnique));
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.Swagger2)]
    [InlineData(SchemaType.OpenApi3)]
    public void Enum_ExplicitNullable_AllowsNull(SchemaType schemaType)
    {
        // Arrange
        var schema = new JsonSchema { Type = JsonObjectType.String | JsonObjectType.Null, IsNullableRaw = true };
        schema.Enumeration.Add("value");

        // Act
        var errors = new JsonSchemaValidator().Validate("null", schema, schemaType);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Enum_PublicValues_PreserveStructureAndNodeOwnership()
    {
        // Arrange
        var owner = JsonNode.Parse("{\"value\":{\"b\":null,\"a\":1.0}}");
        using var document = JsonDocument.Parse("{\"a\":1,\"b\":null}");
        var values = new object[]
        {
            new Dictionary<string, object> { ["a"] = 1m, ["b"] = null },
            document.RootElement,
            owner["value"]
        };

        foreach (var value in values)
        {
            var schema = new JsonSchema();
            schema.Enumeration.Add(value);

            // Act
            var errors = schema.Validate("{\"b\":null,\"a\":1e0}");

            // Assert
            Assert.Empty(errors);
            Assert.Contains(schema.Validate("{\"a\":1}"), error => error.Kind == ValidationErrorKind.NotInEnumeration);
        }
        Assert.Same(owner, owner["value"].Parent);
    }

    [Fact]
    public void Enum_PublicArrayAndPrimitives_UseJsonIdentity()
    {
        // Arrange
        var schema = new JsonSchema();
        schema.Enumeration.Add(new object[] { (byte)1, (uint)2, 3m, true, null, "4" });

        // Act
        var errors = schema.Validate("[1.0,2e0,3,true,null,\"4\"]");

        // Assert
        Assert.Empty(errors);
        Assert.Contains(schema.Validate("[1,2,3,true,null,4]"), error => error.Kind == ValidationErrorKind.NotInEnumeration);
    }
}
