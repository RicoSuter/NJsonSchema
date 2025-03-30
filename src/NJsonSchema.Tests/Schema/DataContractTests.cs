using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Schema
{
    public class DataContractTests
    {
        public class MissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_DataContractAttribute_is_missing_then_DataMember_is_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MissingDataContract>();

            // Assert
            Assert.True(schema.Properties.ContainsKey("Bar"));
        }


        [DataContract]
        public class NotMissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }

            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_DataContractAttribute_is_not_missing_then_DataMember_is_checked()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<NotMissingDataContract>();

            // Assert
            Assert.True(schema.Properties.ContainsKey("bar"));
            Assert.False(schema.Properties.ContainsKey("Foo"));
        }


        [DataContract]
        public class DataContractWithoutDataMember
        {
            public string Bar { get; set; }
        }


        [Fact]
        public async Task When_class_has_DataContractAttribute_then_properties_without_DataMemberAttributes_are_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DataContractWithoutDataMember>();

            // Assert
            Assert.Empty(schema.Properties);
        }


        [DataContract]
        public class DataContractWithoutDataMemberWithJsonProperty
        {
            [JsonProperty("bar")]
            public string Bar { get; set; }
        }
        
        [Fact]
        public async Task When_class_has_DataContractAttribute_then_property_without_DataMemberAttribute_and_with_JsonPropertyAttribute_is_not_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DataContractWithoutDataMemberWithJsonProperty>();

            // Assert
            Assert.True(schema.Properties.ContainsKey("bar"));
        }


        [DataContract]
        public class DataContractWitDataMemberWithJsonProperty
        {
            [DataMember]
            [JsonIgnore]
            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_class_has_DataContractAttribute_then_property_with_DataMemberAttribute_and_JsonIgnoreAttribute_is_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DataContractWitDataMemberWithJsonProperty>();

            // Assert
            Assert.Empty(schema.Properties);
        }

        [DataContract]
        public class DataContractWithRequiredProperty
        {
            [DataMember(Name = "req", IsRequired = true)]
            public string Required { get; set; }
        }

        [Fact]
        public async Task When_DataMemberAttribute_is_required_then_schema_property_is_required()
        {
            // Newtonsoft.Json also respects DataMemberAttribute.IsRequired => this throws an exception
            //var json = JsonConvert.DeserializeObject<DataContractWithRequiredProperty>("{}");

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DataContractWithRequiredProperty>();

            // Assert
            Assert.True(schema.Properties["req"].IsRequired);
        }
    }
}