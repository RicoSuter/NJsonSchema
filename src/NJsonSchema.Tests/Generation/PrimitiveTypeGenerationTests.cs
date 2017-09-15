using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NodaTime;

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

            public LocalDate Date { get; set; }

            public ZonedDateTime ZonedDateTime { get; set; }

            public OffsetDateTime OffsetDateTime { get; set; }

            public Duration Duration { get; set; }
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

        [TestMethod]
        public async Task When_property_is_localdate_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Date"].Type);
            Assert.AreEqual(JsonFormatStrings.Date, schema.Properties["Date"].Format);
        }

        [TestMethod]
        public async Task When_property_is_zoneddatetime_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["ZonedDateTime"].Type);
            Assert.AreEqual(JsonFormatStrings.DateTime, schema.Properties["ZonedDateTime"].Format);
        }

        [TestMethod]
        public async Task When_property_is_offsetdatetime_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["OffsetDateTime"].Type);
            Assert.AreEqual(JsonFormatStrings.DateTime, schema.Properties["OffsetDateTime"].Format);
        }

        [TestMethod]
        public async Task When_property_is_duration_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Duration"].Type);
            Assert.AreEqual(JsonFormatStrings.TimeSpan, schema.Properties["Duration"].Format);
        }
    }
}