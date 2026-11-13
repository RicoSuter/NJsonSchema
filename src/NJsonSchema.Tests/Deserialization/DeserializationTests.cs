using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.Tests.Deserialization;

public class DeserializationTests
{
    [Fact]
    public async Task CanRoundTripPayPalOpenApi()
    {
        var schema = await JsonSchema.FromJsonAsync(File.OpenRead(Path.Combine("Deserialization", "TestData", "paypal_billing_subscriptions_v1.json")));

        await VerifyHelper.Verify(schema.ToJson());
    }

    [Fact]
    public async Task When_schema_keyword_is_repeated_then_last_value_is_used()
    {
        // Arrange
        var json = """{ "items": { "type": "string" }, "items": { "type": "integer" } }""";

        // Act
        var schema = await JsonSchema.FromJsonAsync(json);

        // Assert
        Assert.Equal(JsonObjectType.Integer, schema.Item!.Type);
    }
}