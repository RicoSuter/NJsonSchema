using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class FieldGenerationTests
    {
        public class MyTest
        {
            public string MyField;
        }

        [TestMethod]
        public void When_public_field_is_available_then_it_is_added_as_property()
        {
            //// Arrange
            

            //// Act
            var schema = JsonSchema4.FromType<MyTest>();
            var json = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties["MyField"].Type.HasFlag(JsonObjectType.String));
        }
    }
}
