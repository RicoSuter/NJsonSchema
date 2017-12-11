using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
        public async Task When_property_name_is_default_then_schema_has_reflected_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.Default
            });

            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("BarBar1JsonProperty"));
            Assert.Equal("BarBar1JsonProperty", schema.Properties["BarBar1JsonProperty"].Name);
            Assert.True(schema.Properties.ContainsKey("BarBar2DataMember"));
            Assert.Equal("BarBar2DataMember", schema.Properties["BarBar2DataMember"].Name);
        }

        [Fact]
        public async Task When_property_name_is_camel_then_schema_has_camel_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.CamelCase
            });

            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("barBar1JsonProperty"));
            Assert.Equal("barBar1JsonProperty", schema.Properties["barBar1JsonProperty"].Name);
            Assert.True(schema.Properties.ContainsKey("barBar2DataMember"));
            Assert.Equal("barBar2DataMember", schema.Properties["barBar2DataMember"].Name);
        }

        [Fact]
        public async Task When_property_name_is_snake_then_schema_has_snake_names()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.SnakeCase
            });

            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("bar_bar1_json_property"));
            Assert.Equal("bar_bar1_json_property", schema.Properties["bar_bar1_json_property"].Name);
            Assert.True(schema.Properties.ContainsKey("bar_bar2_data_member"));
            Assert.Equal("bar_bar2_data_member", schema.Properties["bar_bar2_data_member"].Name);
        }
    }
}