using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
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

        [Fact]
        public async Task When_array_property_is_not_nullable_then_it_does_not_have_a_setter()
        {
            //// Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyRequiredTest>();
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
            Assert.Contains("public System.Collections.Generic.ICollection<string> Collection { get; } = new System.Collections.ObjectModel.Collection<string>();", code);
            Assert.Contains("public System.Collections.Generic.IDictionary<string, object> Dictionary { get; } = new System.Collections.Generic.Dictionary<string, object>();", code);
        }
    }
}
