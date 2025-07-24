using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

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
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyRequiredTest>();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco, 
                GenerateImmutableArrayProperties = true, 
                GenerateImmutableDictionaryProperties = true, 
                GenerateDefaultValues = false
            });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }
    }
}
