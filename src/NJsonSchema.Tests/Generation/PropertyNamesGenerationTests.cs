using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class PropertyNamesGenerationTests
    {
        [DataContract]
        public class Foo
        {
            [JsonProperty("BARbar1")]
            public string BarBar1 { get; set; }

            [DataMember(Name = "BARbar2")]
            public string BarBar2 { get; set; }
        }

        [TestMethod]
        public void When_property_name_is_default_then_schema_has_reflected_names()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.Default
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("BARbar1"));
            Assert.AreEqual("BARbar1", schema.Properties["BARbar1"].Name);
            Assert.IsTrue(schema.Properties.ContainsKey("BARbar2"));
            Assert.AreEqual("BARbar2", schema.Properties["BARbar2"].Name);
        }

        [TestMethod]
        public void When_property_name_is_camel_then_schema_has_camel_names()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.CamelCase
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("barBar1"));
            Assert.AreEqual("barBar1", schema.Properties["barBar1"].Name);
            Assert.IsTrue(schema.Properties.ContainsKey("barBar2"));
            Assert.AreEqual("barBar2", schema.Properties["barBar2"].Name);
        }
    }
}