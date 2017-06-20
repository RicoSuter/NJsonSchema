using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.Tests.Generation;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class DataContractTests
    {
        public class MissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }
        }

        [TestMethod]
        public async Task When_DataContractAttribute_is_missing_then_DataMember_is_ignored()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MissingDataContract>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("Bar"));
        }


        [DataContract]
        public class NotMissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }

            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_DataContractAttribute_is_not_missing_then_DataMember_is_checked()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<NotMissingDataContract>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("bar"));
            Assert.IsFalse(schema.Properties.ContainsKey("Foo"));
        }


        [DataContract]
        public class DataContractWithoutDataMember
        {
            public string Bar { get; set; }
        }


        [TestMethod]
        public async Task When_class_has_DataContractAttribute_then_properties_without_DataMemberAttributes_are_ignored()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DataContractWithoutDataMember>();

            //// Assert
            Assert.AreEqual(0, schema.Properties.Count);
        }


        [DataContract]
        public class DataContractWithoutDataMemberWithJsonProperty
        {
            [JsonProperty("bar")]
            public string Bar { get; set; }
        }
        
        [TestMethod]
        public async Task When_class_has_DataContractAttribute_then_property_without_DataMemberAttribute_and_with_JsonPropertyAttribute_is_not_ignored()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DataContractWithoutDataMemberWithJsonProperty>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("bar"));
        }


        [DataContract]
        public class DataContractWitDataMemberWithJsonProperty
        {
            [DataMember]
            [JsonIgnore]
            public string Bar { get; set; }
        }

        [TestMethod]
        public async Task When_class_has_DataContractAttribute_then_property_with_DataMemberAttribute_and_JsonIgnoreAttribute_is_ignored()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DataContractWitDataMemberWithJsonProperty>();

            //// Assert
            Assert.AreEqual(0, schema.Properties.Count);
        }

        [DataContract]
        public class DataContractWithRequiredProperty
        {
            [DataMember(Name = "req", IsRequired = true)]
            public string Required { get; set; }
        }

        [TestMethod]
        public async Task When_DataMemberAttribute_is_required_then_schema_property_is_required()
        {
            // Newtonsoft.Json also respects DataMemberAttribute.IsRequired => this throws an exception
            //var json = JsonConvert.DeserializeObject<DataContractWithRequiredProperty>("{}");

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<DataContractWithRequiredProperty>();

            //// Assert
            Assert.IsTrue(schema.Properties["req"].IsRequired);
        }
    }
}