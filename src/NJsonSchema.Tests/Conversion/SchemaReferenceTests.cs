using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Conversion
{
    [TestClass]
    public class SchemaReferenceTests
    {
        [TestMethod]
        public void When_converting_a_circular_referencing_person_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Person>();

            //// Assert
            Assert.AreEqual(schema, schema.Properties["Car"].ActualSchema.Properties["Person"].ActualSchema);
        }

        [TestMethod]
        public void When_converting_a_circular_referencing_car_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Car>();

            //// Assert
            Assert.AreEqual(schema, schema.Properties["Person"].ActualSchema.Properties["Car"].ActualSchema);
        }

        [TestMethod]
        public void When_converting_a_referencing_type_then_path_is_in_json()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Person>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsTrue(json.Contains(@"""$ref"": ""#"""));
        }

        [TestMethod]
        public void When_converting_a_referencing_type_then_absolute_reference_path_is_in_json()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<House>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsTrue(json.Contains(@"""$ref"": ""#/definitions/Person"));
        }
    }

    public class Person
    {
        public string Name { get; set; }

        public Car Car { get; set; }
    }

    public class Car
    {
        public string Name { get; set; }

        public Person Person { get; set; }
    }

    public class House
    {
        public Person Person { get; set; }
    }
}
