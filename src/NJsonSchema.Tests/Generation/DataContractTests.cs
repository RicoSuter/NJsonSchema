using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Generation;
using System.Runtime.Serialization;

namespace NJsonSchema.Tests.Generation
{
    public class DataContractTests
    {
        [DataContract]
        public class Person
        {
            [DataMember(Name = "middleName", IsRequired = true)]
            public int? MiddleName { get; set; }
        }

        [Fact]
        public void When_DataContractRequired_is_set_then_undefined_is_not_allowed()
        {
            // Assert
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Person>(@"{}"));
        }

        [Fact]
        public void When_DataContractRequired_is_set_then_null_is_allowed()
        {
            // Act
            JsonConvert.DeserializeObject<Person>(@"{""middleName"": null}");
            // Assert: Does not throw
        }

        [Fact]
        public void When_DataContractRequired_is_set_property_is_nullable_in_OpenApi3()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.ActualProperties["middleName"].IsRequired);
            Assert.True(schema.ActualProperties["middleName"].IsNullable(SchemaType.OpenApi3));
        }

        [Fact]
        public void When_DataContractRequired_is_set_property_is_nullable_in_Swagger2()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>(new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.Swagger2 });
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.ActualProperties["middleName"].IsRequired);
            Assert.True(schema.ActualProperties["middleName"].IsNullable(SchemaType.Swagger2));

            // The property must be "required" because not setting it would result in a serialization
            // error, but its value may still be null. Swagger2 has no native null, so x-nullable 
            // is emitted to express that the required value can be null.
        }
    }
}
