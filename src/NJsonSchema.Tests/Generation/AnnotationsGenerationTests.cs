using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Annotations;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class AnnotationsGenerationTests
    {
        public class AnnotationClass
        {
            public Point Point { get; set; }
        }

        [JsonSchema(JsonObjectType.String, Format = "point")]
        public class Point
        {
            public decimal X { get; set; }

            public decimal Y { get; set; }
        }

        [TestMethod]
        public void When_annotations_are_available_then_type_and_format_can_be_customized()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<AnnotationClass>();
            var schemaData = schema.ToJson();

            //// Act
            var property = schema.Properties["Point"];

            //// Assert
            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.String));
            Assert.AreEqual("point", property.Format);
        }

        public class MultipleOfClass
        {
            [MultipleOf(4.5)]
            public double Number { get; set; }
        }

        [TestMethod]
        public void When_multipleOf_attribute_is_available_then_value_is_set_in_schema()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<MultipleOfClass>();
            var property = schema.Properties["Number"];

            //// Assert
            Assert.AreEqual(4.5, property.MultipleOf.Value);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel : IEnumerable<string>
        {
            public IEnumerator<string> GetEnumerator() { return null; }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        
        [TestMethod]
        public void When_class_has_array_item_type_defined_then_schema_has_this_item_type()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ArrayModel>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""string""
  }
}", json);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel<T> : List<T>
        {
        }

        [TestMethod]
        public void When_class_has_array_item_type_defined_then_schema_has_this_item_type2()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ArrayModel<string>>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""string""
  }
}", json);
        }
    }
}