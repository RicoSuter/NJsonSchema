using System.Threading.Tasks;
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
        public async Task When_public_field_is_available_then_it_is_added_as_property()
        {
            //// Arrange
            

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyTest>();
            var json = await schema.ToJsonAsync();

            //// Assert
            Assert.IsTrue(schema.Properties["MyField"].Type.HasFlag(JsonObjectType.String));
        }
    }
}
