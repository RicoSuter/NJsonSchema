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
}