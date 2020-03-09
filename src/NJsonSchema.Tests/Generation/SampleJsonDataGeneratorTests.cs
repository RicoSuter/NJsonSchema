using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class SampleJsonDataGeneratorTests
    {
        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public Address MainAddress { get; set; }

            public Address[] Addresses { get; set; }

        }

        public class Address
        {
            public string Street { get; set; }
        }

        [Fact]
        public void When_sample_data_is_generated_from_schema_then_properties_are_set()
        {
            //// Arrange
            var schema = JsonSchema.FromType<Person>();
            var generator = new SampleJsonDataGenerator();

            //// Act
            var token = generator.Generate(schema);
            var obj = token as JObject;

            //// Assert
            Assert.NotNull(obj.Property(nameof(Person.FirstName)));
            Assert.NotNull(obj.Property(nameof(Person.LastName)));
            Assert.NotNull(obj.Property(nameof(Person.MainAddress)));
            Assert.NotNull(obj.Property(nameof(Person.Addresses)));
        }
    }
}
