using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class NullPropertyTests
    {
        public class ClassRoom
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int? Size { get; set; }
        }

        [TestMethod]
        public void When_property_is_nullable_then_property_schema_type_is_also_null()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ClassRoom>();
            
            //// Act
            var json = schema.ToJson();

            //// Assert
            // TODO: Add test
        }
    }
}