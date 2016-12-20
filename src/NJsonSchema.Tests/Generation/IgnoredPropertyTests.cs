using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class IgnoredPropertyTests
    {
        public class Mno
        {
            public string IncludeMe;

            [JsonIgnore]
            public string IgnoreMe;
        }


        [TestMethod]
        public void When_field_has_JsonIgnoreAttribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Mno>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsFalse(json.Contains("IgnoreMe"));
        }

        [DataContract]
        public class Xyz
        {
            [DataMember]
            public string IncludeMe;

            public string IgnoreMe;
        }

        [TestMethod]
        public void When_field_has_no_DataMemberAttribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Xyz>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsFalse(json.Contains("IgnoreMe"));
        }

        public class Foo
        {
            public Dictionary<string, object> DynamicValues { get; set; }

            [JsonIgnore]
            public object this[string key]
            {
                get { throw new NotImplementedException(); }
            }

            public string Bar { get; set; }
        }

        [TestMethod]
        public void When_indexer_property_has_ignore_attribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Foo>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(2, schema.Properties.Count);
        }
    }
}
