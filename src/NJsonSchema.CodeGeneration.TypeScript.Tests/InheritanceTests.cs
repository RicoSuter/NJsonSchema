using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class InheritanceTests
    {
        public class MyContainer
        {
            public EmptyClassInheritingDictionary CustomDictionary { get; set; }
        }

        public sealed class EmptyClassInheritingDictionary : Dictionary<string, object>
        {
        }

        [Fact]
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            //// Act
            var code = generator.GenerateFile("ContainerClass");

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];
            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);

            Assert.DoesNotContain("EmptyClassInheritingDictionary", code);
            Assert.Contains("customDictionary: { [key: string] : any; } | undefined;", code);
        }
    }
}
