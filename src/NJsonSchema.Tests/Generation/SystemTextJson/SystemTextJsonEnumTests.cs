using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NJsonSchema.Tests.Generation.SystemTextJson;

public class SystemTextJsonEnumTests
{
#if NET9_0_OR_GREATER
    
    public class StringJsonStringEnumMemberNameContainer
    {
        public CloudCoverWithJsonStringEnumMemberName CloudCover { get; set; }
    }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CloudCoverWithJsonStringEnumMemberName
    {
        Clear,
        [JsonStringEnumMemberName("Partly cloudy")]
        Partial,
        Overcast
    }
    
    [Fact]
    public Task WhenStringEnumUsesJsonStringEnumMemberName_ThenItIsUsed()
    {
        var schema = JsonSchema.FromType<StringJsonStringEnumMemberNameContainer>();
        return Verify(schema.ToJson()); // should have "Partly cloudy" in "enum" but "Partial" in "x-enumNames"
    }
    
    public class StringEnumMemberContainer
    {
        public CloudCoverWithEnumMember CloudCover { get; set; }
    }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CloudCoverWithEnumMember
    {
        Clear,
        [EnumMember(Value = "Partly cloudy")]
        Partial,
        Overcast
    }
    
    [Fact]
    public Task WhenStringEnumUsesEnumMemberAttribute_ThenItIsUsed()
    {
        var schema = JsonSchema.FromType<StringEnumMemberContainer>();
        return Verify(schema.ToJson()); // should have "Partly cloudy" in "enum" but "Partial" in "x-enumNames"
    }
    
    public class IntegerEnumContainer
    {
        public IntegerCloudCover IntegerCloudCover { get; set; }
    }
 
    // [JsonConverter(typeof(JsonStringEnumConverter))] ==> no converter
    public enum IntegerCloudCover
    {
        Clear,
        [EnumMember(Value = "Partly cloudy")]
        [JsonStringEnumMemberName("Partly cloudy")]
        Partial,
        Overcast
    }
    
    [Fact]
    public Task WhenIntegerEnumUseEnumNameAttributes_ThenTheyAreIgnored()
    {
        var schema = JsonSchema.FromType<IntegerEnumContainer>();
        return Verify(schema.ToJson()); // should have 1 and no "Partly cloudy"
    }

#endif
}