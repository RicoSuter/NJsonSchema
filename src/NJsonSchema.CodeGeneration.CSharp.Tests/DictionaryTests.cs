using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class DictionaryTests
    {
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
            CodeCompiler.AssertCompile(code);
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
