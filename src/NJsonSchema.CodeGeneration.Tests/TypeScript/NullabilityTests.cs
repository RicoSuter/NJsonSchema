using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.Tests.Models;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class NullabilityTests
    {
        [TestMethod]
        public async Task Strict_nullability_in_TypeScript2()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>(
                new JsonSchemaGeneratorSettings {
                    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull
                });

            var generator = new TypeScriptGenerator(schema,
                new TypeScriptGeneratorSettings { TypeScriptVersion = 2 });

            var output = generator.GenerateFile("MyClass");

            Assert.IsTrue(output.Contains("timeSpan: string;"));
            Assert.IsTrue(output.Contains("gender: Gender;"));
            Assert.IsTrue(output.Contains("address: Address;"));

            Assert.IsTrue(output.Contains("timeSpanOrNull: string | undefined;"));
            Assert.IsTrue(output.Contains("genderOrNull: Gender | undefined;"));
            Assert.IsTrue(output.Contains("addressOrNull: Address | undefined;"));
        }
    }
}
