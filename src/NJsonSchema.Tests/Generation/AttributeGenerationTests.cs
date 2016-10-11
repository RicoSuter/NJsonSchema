using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class AttributeGenerationTests
    {
        [TestMethod]
        public void When_minLength_and_maxLength_attribute_are_set_on_array_then_minItems_and_maxItems_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Items"];

            //// Assert
            Assert.AreEqual(3, property.MinItems);
            Assert.AreEqual(5, property.MaxItems);
        }

        [TestMethod]
        public void When_minLength_and_maxLength_attribute_are_set_on_string_then_minLength_and_maxLenght_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["String"];

            //// Assert
            Assert.AreEqual(3, property.MinLength);
            Assert.AreEqual(5, property.MaxLength);
        }

        [TestMethod]
        public void When_Range_attribute_is_set_on_double_then_minimum_and_maximum_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Double"];

            //// Assert
            Assert.AreEqual(5, property.Minimum);
            Assert.AreEqual(10, property.Maximum);
        }

        [TestMethod]
        public void When_Range_attribute_is_set_on_integer_then_minimum_and_maximum_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Integer"];

            //// Assert
            Assert.AreEqual(5, property.Minimum);
            Assert.AreEqual(10, property.Maximum);
        }

        [TestMethod]
        public void When_display_attribute_is_available_then_name_and_description_are_read()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Display"];

            //// Assert
            Assert.AreEqual("Foo", property.Title);
            Assert.AreEqual("Bar", property.Description);
        }

        [TestMethod]
        public void When_description_attribute_is_available_then_description_are_read()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Description"];

            //// Assert
            Assert.AreEqual("Abc", property.Description);
        }

        [TestMethod]
        public void When_required_attribute_is_available_then_property_is_required()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Required"];

            //// Assert
            Assert.IsTrue(property.IsRequired);
        }

        [TestMethod]
        public void When_required_attribute_is_not_available_then_property_is_can_be_null()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["Description"];

            //// Assert
            Assert.IsFalse(property.IsRequired);
            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.Null));
        }

        [TestMethod]
        public void When_ReadOnly_is_set_then_readOnly_is_set_in_schema()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<AttributeTestClass>();
            var property = schema.Properties["ReadOnly"];

            //// Assert
            Assert.IsTrue(property.IsReadOnly);
        }

        public class AttributeTestClass
        {
#if !LEGACY
            [MinLength(3)]
            [MaxLength(5)]
#endif
            public string[] Items { get; set; }

#if !LEGACY
            [MinLength(3)]
            [MaxLength(5)]
#endif
            public string String { get; set; }

            [Range(5, 10)]
            public double Double { get; set; }

            [Range(5, 10)]
            public int Integer { get; set; }

            [Display(Name = "Foo", Description = "Bar")]
            public string Display { get; set; }

            [System.ComponentModel.Description("Abc")]
            public string Description { get; set; }

            [Required]
            public bool Required { get; set; }

            [ReadOnly(true)]
            public bool ReadOnly { get; set; }
        }
    }
}