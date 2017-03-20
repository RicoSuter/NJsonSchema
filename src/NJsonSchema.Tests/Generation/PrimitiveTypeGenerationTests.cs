using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class PrimitiveTypeGenerationTests
    {
        public class Foo
        {
            [Required]
            public byte[] Bytes { get; set; }

            public byte Byte { get; set; }

            public TimeSpan TimeSpan { get; set; }

            [Required]
            public Type Type { get; set; }
        }

        [TestMethod]
        public async Task When_property_is_byte_array_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Bytes"].Type);
            Assert.AreEqual(JsonFormatStrings.Byte, schema.Properties["Bytes"].Format);
        }

        [TestMethod]
        public async Task When_property_is_byte_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["Byte"].Type);
            Assert.AreEqual(JsonFormatStrings.Byte, schema.Properties["Byte"].Format);
        }

        [TestMethod]
        public async Task When_property_is_timespan_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["TimeSpan"].Type);
            Assert.AreEqual(JsonFormatStrings.TimeSpan, schema.Properties["TimeSpan"].Format);
        }

        [TestMethod]
        public async Task When_property_is_type_then_schema_type_is_string()
        {
            //// Arrange
            var data = JsonConvert.SerializeObject(new Foo { Type = typeof(Foo) }); // Type property is serialized as string

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Type"].Type);
        }
    }
}