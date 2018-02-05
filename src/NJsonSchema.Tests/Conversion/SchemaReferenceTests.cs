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

        [Fact]
        public async Task When_ref_is_nested_then_it_should_be_resolved()
        {
            /// Arrange
            var json = @"
{
    ""$schema"": ""http://json-schema.org/draft-04/schema#"",
    ""$ref"": ""#/definitions/refOne"",
    ""definitions"": {
        ""refOne"": {
            ""type"": ""object"",
            ""properties"": {
                ""Two"": {
                    ""$ref"": ""#/definitions/refTwo""
                }
            }
        },
        ""refTwo"": {
            ""type"": ""object"",
            ""properties"": {
                ""Id"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";

            /// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            /// Assert
            var jsonOutput = schema.ToJson();
            Assert.NotNull(jsonOutput);
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
