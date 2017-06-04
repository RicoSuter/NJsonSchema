using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class ShouldSerializeTests
    {
        public class Test
        {
            public string Name { get; set; }

            public bool ShouldSerializeName()
            {
                return !string.IsNullOrEmpty(Name);
            }
        }

        [TestMethod]
        public void When_ShouldSerialize_method_exists_then_schema_is_generated()
        {
            //// Arrange
            var schema = JsonSchema4.FromTypeAsync<Test>();

            //// Act


            //// Assert
            Assert.IsNotNull(schema);
        }
    }
}
