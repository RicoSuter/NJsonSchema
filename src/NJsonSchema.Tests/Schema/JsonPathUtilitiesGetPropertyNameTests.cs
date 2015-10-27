using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class JsonPathUtilitiesGetPropertyNameTests
    {
        public class MissingDataContract
        {
            [DataMember(Name = "bar")]
            public string Bar { get; set; }
        }

        [DataContract]
        public class NotMissingDataContract
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

        [TestMethod]
        public void When_DataContractAttribute_is_not_missing_then_DataMember_is_checked()
        {
            //// Act
            var schema = JsonSchema4.FromType<NotMissingDataContract>();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("bar"));
        }
    }
}