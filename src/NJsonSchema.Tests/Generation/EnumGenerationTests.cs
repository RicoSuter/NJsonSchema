using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class EnumGenerationTests
    {
        public class Foo
        {
            public Bar Bar { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Bar Bar2 { get; set; }
        }

        public enum Bar
        {
            A = 0, 
            B = 5, 
            C = 6, 
        }

        [TestMethod]
        public void When_property_is_integer_enum_then_schmea_has_enum()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson(); 

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["Bar"].Type);
            Assert.AreEqual(3, schema.Properties["Bar"].ActualSchema.Enumeration.Count);
            Assert.AreEqual(0, schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(0));
            Assert.AreEqual(5, schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(1));
            Assert.AreEqual(6, schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(2));
        }

        [TestMethod]
        public void When_string_and_integer_enum_used_then_two_refs_are_generated()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Assert
            Assert.IsNotNull(schema.Properties["Bar"].SchemaReference);
            Assert.IsNotNull(schema.Properties["Bar2"].SchemaReference); // must not be a reference but second enum declaration
            Assert.AreNotEqual(schema.Properties["Bar"].SchemaReference, schema.Properties["Bar2"].SchemaReference);
        }

        [TestMethod]
        public void When_property_is_string_enum_then_schema_has_enum()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String
            });

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Bar"].Type);
            Assert.AreEqual(3, schema.Properties["Bar"].ActualSchema.Enumeration.Count);
            Assert.AreEqual("A", schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(0));
            Assert.AreEqual("B", schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(1));
            Assert.AreEqual("C", schema.Properties["Bar"].ActualSchema.Enumeration.ElementAt(2));
        }

        [TestMethod]
        public void When_enum_is_generated_then_names_are_set()
        {
            //// Arrange


            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });

            //// Assert
            Assert.AreEqual(3, schema.Properties["Bar"].ActualSchema.EnumerationNames.Count);
            Assert.AreEqual("A", schema.Properties["Bar"].ActualSchema.EnumerationNames.ElementAt(0));
            Assert.AreEqual("B", schema.Properties["Bar"].ActualSchema.EnumerationNames.ElementAt(1));
            Assert.AreEqual("C", schema.Properties["Bar"].ActualSchema.EnumerationNames.ElementAt(2));
        }
    }
}