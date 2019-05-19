using NJsonSchema.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

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
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ArrayTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                ArrayType = "Foo",
                ArrayInstanceType = "Bar"
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public Foo<string> ArrayProperty { get; set; } = new Bar<string>();", code);
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
            var schema = await JsonSchema4.FromTypeAsync<ClassWithNullableArrayItems>();
            var json = schema.ToJson();
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings());

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.True(schema.Properties["Items"].Item.IsNullable(SchemaType.JsonSchema));
            Assert.Contains("System.Collections.ObjectModel.ObservableCollection<int?> Items", output);
        }
    }
}