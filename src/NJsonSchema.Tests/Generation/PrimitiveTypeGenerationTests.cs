using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class PrimitiveTypeGenerationTests
    {
        public class Foo
        {
            public byte[] Bytes { get; set; }

            public byte Byte { get; set; }

            public TimeSpan TimeSpan { get; set; }
        }

        [TestMethod]
        public void When_property_is_byte_array_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Bytes"].Type);
            Assert.AreEqual(JsonFormatStrings.Base64, schema.Properties["Bytes"].Format);
        }

        [TestMethod]
        public void When_property_is_byte_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["Byte"].Type);
            Assert.AreEqual(JsonFormatStrings.Byte, schema.Properties["Byte"].Format);
        }

        [TestMethod]
        public void When_property_is_timespan_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["TimeSpan"].Type);
            Assert.AreEqual(JsonFormatStrings.TimeSpan, schema.Properties["TimeSpan"].Format);
        }
    }
}