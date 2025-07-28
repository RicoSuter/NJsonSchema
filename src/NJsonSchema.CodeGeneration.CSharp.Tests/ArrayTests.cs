using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel.DataAnnotations;
using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class ArrayTests
    {
        public class ArrayTest
        {
            [Required]
            public List<string> ArrayProperty { get; set; }
        }

        [Fact]
        public async Task When_array_property_is_required_then_array_instance_can_be_changed()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ArrayTest>();
            var data = schema.ToJson();

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                ArrayType = "Foo",
                ArrayInstanceType = "Bar"
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }

        public class ClassWithNullableArrayItems
        {
            [NotNull]
            [ItemsCanBeNull]
            public List<int?> Items { get; set; }
        }

        [Fact]
        public async Task When_array_item_is_nullable_then_generated_CSharp_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithNullableArrayItems>();
            var json = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.True(schema.Properties["Items"].Item.IsNullable(SchemaType.JsonSchema));
            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
        }
    }
}