using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ClassOrderTests
    {
        [Fact]
        public void When_class_order_is_wrong_then_classes_are_correctly_reordered()
        {
            //// Arrange
            var classes = new List<CodeArtifact>
            {
                new CodeArtifact("Car", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, ""),
                new CodeArtifact("Apple", "List<Fruit>", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, ""),
                new CodeArtifact("Professor", "Teacher", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, ""),
                new CodeArtifact("Teacher", "Person[]", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, ""),
                new CodeArtifact("Fruit", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, ""),
                new CodeArtifact("Person", CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Undefined, "")
            };

            //// Act
            classes = classes.OrderByBaseDependency().ToList();
            var order = string.Join(", ", classes.Select(c => c.TypeName));

            //// Assert
            Assert.Equal("Car, Fruit, Apple, Person, Teacher, Professor", order);
        }
    }
}
