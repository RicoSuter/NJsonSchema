using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
        public void When_two_classes_inherit_common_base_class_then_it_is_the_common_base_type()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Dog), typeof(Horse) }.FindCommonBaseType();

            //// Assert
            Assert.Equal(typeof(Animal), baseType);
        }

        [Fact]
        public void When_one_class_is_base_class_then_it_is_the_common_base_class()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Animal), typeof(Horse) }.FindCommonBaseType();

            //// Assert
            Assert.Equal(typeof(Animal), baseType);
        }

        [Fact]
        public void When_no_common_base_class_exists_then_object_is_common_base_class()
        {
            //// Arrange


            //// Act
            var baseType = new[] { typeof(Animal), typeof(Horse), typeof(FindCommonBaseTypeTests) }.FindCommonBaseType();

            //// Assert
            Assert.Equal(typeof(object), baseType);
        }
    }
}
