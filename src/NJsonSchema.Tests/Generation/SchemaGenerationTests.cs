using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class SchemaGenerationTests
    {
        public class Foo
        {
            public Dictionary<string, string> Dictionary { get; set; }

            public Bar Bar { get; set; }

            public DateTimeOffset Time { get; set; }
        }

        public class Bar
        {
            public string Name { get; set; }
        }
        
        [TestMethod]
        public void When_generating_schema_with_object_property_then_additional_properties_are_not_allowed()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(false, schema.Properties["Bar"].AllowAdditionalProperties);
        }

        [TestMethod]
        public void When_generating_DateTimeOffset_property_then_format_datetime_must_be_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Time"].Type);
            Assert.AreEqual(JsonFormatStrings.DateTime, schema.Properties["Time"].Format);
        }

        [TestMethod]
        public void When_generating_schema_with_dictionary_property_then_it_must_allow_additional_properties()
        {
            //// Arrange
            
            //// Act
            var schema = JsonSchema4.FromType<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(true, schema.Properties["Dictionary"].AllowAdditionalProperties);
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Dictionary"].AdditionalPropertiesSchema.Type);
        }

        public class DefaultTests
        {
            [DefaultValue(10)]
            public int Number { get; set; }
        }
        
        [TestMethod]
        public void When_default_value_is_set_on_property_then_default_is_set_in_schema()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<DefaultTests>();

            //// Act
            var property = schema.Properties["Number"];

            //// Assert
            Assert.AreEqual(10, property.Default);
        }
    }
}
