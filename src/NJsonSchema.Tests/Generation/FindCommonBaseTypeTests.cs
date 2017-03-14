using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class FindCommonBaseTypeTests
    {
        public class Dog : Animal
        {
        }

        public class Horse : Animal
        {
        }

        public class Animal
        {
        }

        [TestMethod]
        public void When_two_classes_inherit_common_base_class_then_it_is_the_common_base_type()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Dog), typeof(Horse) }.FindCommonBaseType();

            //// Assert
            Assert.AreEqual(typeof(Animal), baseType);
        }

        [TestMethod]
        public void When_one_class_is_base_class_then_it_is_the_common_base_class()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Animal), typeof(Horse) }.FindCommonBaseType();

            //// Assert
            Assert.AreEqual(typeof(Animal), baseType);
        }

        [TestMethod]
        public void When_no_common_base_class_exists_then_object_is_common_base_class()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Animal), typeof(Horse), typeof(FindCommonBaseTypeTests) }.FindCommonBaseType();

            //// Assert
            Assert.AreEqual(typeof(object), baseType);
        }
    }
}
