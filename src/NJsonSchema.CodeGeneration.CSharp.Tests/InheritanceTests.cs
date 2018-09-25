using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
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

            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];
            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);

            Assert.Contains("public EmptyClassInheritingDictionary CustomDictionary", code);
            Assert.Contains("public partial class EmptyClassInheritingDictionary : System.Collections.Generic.Dictionary<string, object>", code);
        }
    }
}
