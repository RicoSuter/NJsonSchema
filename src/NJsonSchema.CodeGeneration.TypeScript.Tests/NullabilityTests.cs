using NJsonSchema.CodeGeneration.TypeScript.Tests.Models;
using NJsonSchema.Generation;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class NullabilityTests
    {
        [Fact]
        public async Task Strict_nullability_in_TypeScript2()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>(
                new JsonSchemaGeneratorSettings {
                    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull
                });

            var generator = new TypeScriptGenerator(schema,
                new TypeScriptGeneratorSettings { TypeScriptVersion = 2 });

            var output = generator.GenerateFile("MyClass");

            Assert.Contains("timeSpan: string;", output);
            Assert.Contains("gender: Gender;", output);
            Assert.Contains("address: Address;", output);
            Assert.Contains("this.address = new Address();", output);

            Assert.Contains("timeSpanOrNull: string | undefined;", output);
            Assert.Contains("genderOrNull: Gender | undefined;", output);
            Assert.Contains("addressOrNull: Address | undefined;", output);
        }
    }
}
