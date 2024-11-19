using System.Threading.Tasks;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

using static NJsonSchema.CodeGeneration.TypeScript.Tests.VerifyHelper;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests;

public class PropertyNameTests
{
    private class TypeWithRestrictedProperties
    {
        public string Constructor { get; set; }
        public string Init { get; set; }
        public string FromJS { get; set; }
        public string ToJSON { get; set; }
    }

    [Fact]
    public async Task When_class_has_restricted_properties_they_are_escaped()
    {
        var schema = NewtonsoftJsonSchemaGenerator.FromType<TypeWithRestrictedProperties>();

        var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 4.3m });
        var output = generator.GenerateFile(nameof(TypeWithRestrictedProperties));

        await Verify(output);
    }
}