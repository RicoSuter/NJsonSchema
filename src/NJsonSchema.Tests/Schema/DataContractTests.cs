using System.Runtime.Serialization;
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
        public void When_DataContractAttribute_is_missing_then_DataMember_is_ignored()
        {
            //// Act
            var schema = JsonSchema4.FromType<MissingDataContract>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("Bar"));
        }


        [DataContract]
        public class NotMissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }
        }

        [TestMethod]
        public void When_DataContractAttribute_is_not_missing_then_DataMember_is_checked()
        {
            //// Act
            var schema = JsonSchema4.FromType<NotMissingDataContract>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("bar"));
        }


        [DataContract]
        public class DataContractWithoutDataMember
        {
            public string Bar { get; set; }
        }


        [TestMethod]
        public void When_class_has_DataContractAttribute_then_properties_without_DataMemberAttributes_are_ignored()
        {
            //// Act
            var schema = JsonSchema4.FromType<DataContractWithoutDataMember>();

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
        public void When_class_has_DataContractAttribute_then_property_without_DataMemberAttribute_and_with_JsonPropertyAttribute_is_not_ignored()
        {
            //// Act
            var schema = JsonSchema4.FromType<DataContractWithoutDataMemberWithJsonProperty>();

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
        public void When_class_has_DataContractAttribute_then_property_with_DataMemberAttribute_and_JsonIgnoreAttribute_is_ignored()
        {
            //// Act
            var schema = JsonSchema4.FromType<DataContractWitDataMemberWithJsonProperty>();

            //// Assert
            Assert.AreEqual(0, schema.Properties.Count);
        }
    }
}