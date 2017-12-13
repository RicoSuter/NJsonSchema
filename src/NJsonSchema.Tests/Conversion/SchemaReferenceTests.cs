using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Conversion
{
    public class SchemaReferenceTests
    {
        [Fact]
        public async Task When_converting_a_circular_referencing_person_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Person>();
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(schema, schema.Properties["Car"].ActualTypeSchema.Properties["Person"].ActualTypeSchema);
        }

        [Fact]
        public async Task When_converting_a_circular_referencing_car_type_then_references_are_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Car>();

            //// Assert
            Assert.Equal(schema, schema.Properties["Person"].ActualTypeSchema.Properties["Car"].ActualTypeSchema);
        }

        [Fact]
        public async Task When_converting_a_referencing_type_then_path_is_in_json()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Person>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.Contains(@"""$ref"": ""#""", json);
        }

        [Fact]
        public async Task When_converting_a_referencing_type_then_absolute_reference_path_is_in_json()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<House>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.Contains(@"""$ref"": ""#/definitions/Person", json);
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
