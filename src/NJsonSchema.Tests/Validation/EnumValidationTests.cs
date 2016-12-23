using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class EnumValidationTests
    {
        [TestMethod]
        public async Task When_enum_is_defined_without_type_then_validation_succeeds_for_correct_value()
        {
            //// Arrange
            var json = 
            @"{
                ""enum"": [
                    ""commercial"",
                    ""residential""
                ]
            }";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"""commercial""");

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public async Task When_enum_is_defined_without_type_then_validation_fails_for_wrong_value()
        {
            //// Arrange
            var json =
            @"{
                ""enum"": [
                    ""commercial"",
                    ""residential""
                ]
            }";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"""wrong""");

            //// Assert
            Assert.AreEqual(1, errors.Count);
        }
    }
}
