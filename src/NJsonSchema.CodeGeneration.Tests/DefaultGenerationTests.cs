using System.ComponentModel;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests
{
    public class DefaultGenerationTests
    {
        public class DefaultPropertyGenerationClass
        {
            [DefaultValue("foo")]
            public string Test { get; set; }
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_is_in_schema()
        {
            // Arrange
            
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DefaultPropertyGenerationClass>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Assert
            Assert.Equal("foo", schema.Properties["Test"].Default); 
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_value_is_set_in_generated_INPC_CSharp_code()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DefaultPropertyGenerationClass>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Inpc;
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("private string _test = \"foo\";", code);
        }

        [Fact]
        public async Task When_property_has_default_attribute_then_default_value_is_set_in_generated_Poco_CSharp_code()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DefaultPropertyGenerationClass>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Act
            var generator = new CSharpGenerator(schema);
            generator.Settings.ClassStyle = CSharpClassStyle.Poco;
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }
    }
}