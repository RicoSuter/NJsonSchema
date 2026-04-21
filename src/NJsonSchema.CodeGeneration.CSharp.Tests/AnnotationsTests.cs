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
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_custom_class_and_property_annotation_templates_are_used_then_annotations_are_on_separate_lines()
        {
            // Arrange
            var json = @"{ 'properties': { 'name': { 'type': 'string' } } }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var tempDir = Path.Combine(Path.GetTempPath(), "NJsonSchema_AnnotationTest_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "Class.Annotations.liquid"), "[System.Runtime.Serialization.DataContract]");
                File.WriteAllText(Path.Combine(tempDir, "Class.Property.Annotations.liquid"), "[System.Runtime.Serialization.DataMember]");

                var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
                {
                    ClassStyle = CSharpClassStyle.Poco,
                    Namespace = "TestNs",
                    TemplateDirectory = tempDir
                });

                // Act
                var code = generator.GenerateFile("MyClass");

                // Assert - annotations should be on their own lines, not on the same line as class/property declarations
                Assert.Contains("[System.Runtime.Serialization.DataContract]\n    public partial class MyClass", code);
                Assert.Contains("[System.Runtime.Serialization.DataMember]\n        public string Name", code);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
