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
                new CodeArtifact("Car", CodeArtifactType.Class, CodeArtifactLanguage.CSharp),
                new CodeArtifact("Apple", "List<Fruit>", CodeArtifactType.Class, CodeArtifactLanguage.CSharp),
                new CodeArtifact("Professor", "Teacher", CodeArtifactType.Class, CodeArtifactLanguage.CSharp),
                new CodeArtifact("Teacher", "Person[]", CodeArtifactType.Class, CodeArtifactLanguage.CSharp),
                new CodeArtifact("Fruit", CodeArtifactType.Class, CodeArtifactLanguage.CSharp),
                new CodeArtifact("Person", CodeArtifactType.Class, CodeArtifactLanguage.CSharp)
            };

            //// Act
            classes = CodeArtifactCollection.OrderByBaseDependency(classes).ToList();
            var order = string.Join(", ", classes.Select(c => c.TypeName));

            //// Assert
            Assert.Equal("Car, Fruit, Apple, Person, Teacher, Professor", order);
        }
    }
}
