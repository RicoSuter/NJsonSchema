using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class DictionaryTests
    {
        [Theory]
        [InlineData("readOnly")]
        [InlineData("readonly")]
        [InlineData("READONLY")]
        public async Task Parsed_readonly_dictionary_preserves_generated_contract(string spelling)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync($$$$$$"""{"type":"object","properties":{"values":{"type":"object","{{{{{{spelling}}}}}}":true,"additionalProperties":{"type":"string"}}}}""");

            // Act
            var output = new CSharpGenerator(schema).GenerateFile("Container");

            // Assert
            Assert.True(schema.Properties["values"].IsReadOnly);
            Assert.Contains("IDictionary<string, string> Values", output);
            var assembly = CSharpCompiler.AssertCompile(output, returnAssembly: true);
            var property = assembly.GetType("MyNamespace.Container")!.GetProperty("Values")!;
            Assert.True(property.GetMethod!.IsPublic);
            Assert.True(property.SetMethod!.IsPublic);
        }

        public enum PropertyName
        {
            Name,
            Gender
        }

        public class EnumKeyDictionaryTest
        {
            public Dictionary<PropertyName, string> EnumDictionary { get; set; }

            [Required]
            public IDictionary<PropertyName, string> EnumInterfaceDictionary { get; set; }
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_csharp_has_enum_key()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_dictionary_property_is_required_then_dictionary_instance_can_be_changed()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                DictionaryType = "Foo",
                DictionaryInstanceType = "Bar"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }
    }
}
