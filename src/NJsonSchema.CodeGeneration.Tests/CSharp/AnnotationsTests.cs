using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class AnnotationsTests
    {
        public class MyRequiredTest
        {
            public string Name { get; set; }

            [Required]
            public List<string> Collection { get; set; }

            [Required]
            public Dictionary<string, object> Dictionary { get; set; }
        }

        [TestMethod]
        public async Task When_array_property_is_not_nullable_then_it_does_not_have_a_setter()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyRequiredTest>();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco, 
                GenerateImmutableArrayProperties = true, 
                GenerateImmutableDictionaryProperties = true, 
                GenerateDefaultValues = false
            });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("public System.Collections.ObjectModel.ObservableCollection<string> Collection { get; } = new System.Collections.ObjectModel.ObservableCollection<string>();"));
            Assert.IsTrue(code.Contains("public System.Collections.Generic.Dictionary<string, object> Dictionary { get; } = new System.Collections.Generic.Dictionary<string, object>();"));
        }
    }
}
