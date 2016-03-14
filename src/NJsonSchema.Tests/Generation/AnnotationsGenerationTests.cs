using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Annotations;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class AnnotationsGenerationTests
    {
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
    }
}