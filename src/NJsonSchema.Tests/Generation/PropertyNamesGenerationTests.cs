using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class PropertyNamesGenerationTests
    {
        [DataContract]
        public class Foo
        {
            [JsonProperty("BarBar1JsonProperty")]
            public string BarBar1 { get; set; }

            [DataMember(Name = "BarBar2DataMember")]
            public string BarBar2 { get; set; }
        }

        [TestMethod]
        public async Task When_property_name_is_default_then_schema_has_reflected_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                //DefaultPropertyNameHandling = PropertyNameHandling.Default
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("BarBar1JsonProperty"));
            Assert.AreEqual("BarBar1JsonProperty", schema.Properties["BarBar1JsonProperty"].Name);
            Assert.IsTrue(schema.Properties.ContainsKey("BarBar2DataMember"));
            Assert.AreEqual("BarBar2DataMember", schema.Properties["BarBar2DataMember"].Name);
        }

        [TestMethod]
        public async Task When_property_name_is_camel_then_schema_has_camel_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
                //DefaultPropertyNameHandling = PropertyNameHandling.CamelCase
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("barBar1JsonProperty"));
            Assert.AreEqual("barBar1JsonProperty", schema.Properties["barBar1JsonProperty"].Name);
            Assert.IsTrue(schema.Properties.ContainsKey("barBar2DataMember"));
            Assert.AreEqual("barBar2DataMember", schema.Properties["barBar2DataMember"].Name);
        }

        [TestMethod]
        public async Task When_property_name_is_snake_then_schema_has_snake_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
                //DefaultPropertyNameHandling = PropertyNameHandling.SnakeCase
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("bar_bar1_json_property"));
            Assert.AreEqual("bar_bar1_json_property", schema.Properties["bar_bar1_json_property"].Name);
            Assert.IsTrue(schema.Properties.ContainsKey("bar_bar2_data_member"));
            Assert.AreEqual("bar_bar2_data_member", schema.Properties["bar_bar2_data_member"].Name);
        }
    }
}