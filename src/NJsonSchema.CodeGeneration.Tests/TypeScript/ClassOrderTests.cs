using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ClassOrderTests
    {
        [TestMethod]
        public void When_class_order_is_wrong_then_classes_are_correctly_reordered()
        {
            //// Arrange
            var classes = new List<TypeGeneratorResult>
            {
                new TypeGeneratorResult
                {
                    TypeName = "Car"
                },
                new TypeGeneratorResult
                {
                    TypeName = "Apple",
                    BaseTypeName = "Fruit"
                },
                new TypeGeneratorResult
                {
                    TypeName = "Professor",
                    BaseTypeName = "Teacher"
                },
                new TypeGeneratorResult
                {
                    TypeName = "Teacher",
                    BaseTypeName = "Person"
                },
                new TypeGeneratorResult
                {
                    TypeName = "Fruit"
                },
                new TypeGeneratorResult
                {
                    TypeName = "Person"
                }
            };

            //// Act
            classes = ClassOrderUtilities.Order(classes).ToList();
            var order = string.Join(", ", classes.Select(c => c.TypeName));

            //// Assert
            Assert.AreEqual("Car, Fruit, Apple, Person, Teacher, Professor", order);
        }
    }
}
