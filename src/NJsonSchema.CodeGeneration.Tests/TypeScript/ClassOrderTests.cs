using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ClassOrderTests
    {
        [TestMethod]
        public void When_class_order_is_wrong_then_classes_are_correctly_reordered()
        {
            //// Arrange
            var classes = new List<CodeArtifact>
            {
                new CodeArtifact
                {
                    TypeName = "Car"
                },
                new CodeArtifact
                {
                    TypeName = "Apple",
                    BaseTypeName = "Fruit"
                },
                new CodeArtifact
                {
                    TypeName = "Professor",
                    BaseTypeName = "Teacher"
                },
                new CodeArtifact
                {
                    TypeName = "Teacher",
                    BaseTypeName = "Person"
                },
                new CodeArtifact
                {
                    TypeName = "Fruit"
                },
                new CodeArtifact
                {
                    TypeName = "Person"
                }
            };

            //// Act
            classes = CodeArtifactCollection.OrderByBaseDependency(classes).ToList();
            var order = string.Join(", ", classes.Select(c => c.TypeName));

            //// Assert
            Assert.AreEqual("Car, Fruit, Apple, Person, Teacher, Professor", order);
        }
    }
}
