using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Conversion
{
    [TestClass]
    public class SchemaReferenceTests
    {
        [TestMethod]
        public async Task When_converting_a_circular_referencing_person_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Person>();
            var json = await schema.ToJsonAsync();

            //// Assert
            Assert.AreEqual(schema, schema.Properties["Car"].ActualPropertySchema.Properties["Person"].ActualPropertySchema);
        }

        [TestMethod]
        public async Task When_converting_a_circular_referencing_car_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Car>();

            //// Assert
            Assert.AreEqual(schema, schema.Properties["Person"].ActualPropertySchema.Properties["Car"].ActualPropertySchema);
        }

        [TestMethod]
        public async Task When_converting_a_referencing_type_then_path_is_in_json()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Person>();

            //// Act
            var json = await schema.ToJsonAsync();

            //// Assert
            Assert.IsTrue(json.Contains(@"""$ref"": ""#"""));
        }

        [TestMethod]
        public async Task When_converting_a_referencing_type_then_absolute_reference_path_is_in_json()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<House>();

            //// Act
            var json = await schema.ToJsonAsync();

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
