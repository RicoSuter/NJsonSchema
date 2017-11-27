using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace NJsonSchema.Tests.Validation
{
    [TestClass]
    public class NumberTests
    {
        [TestMethod]
        public async Task When_double_is_bigger_then_decimal_then_validation_works()
        {
            /// Arrange
            const string json = @"{
                'schema': 'http://json-schema.org/draft-04/schema',
                'title': 'NumberWithCircleVisualisationData',
                'type': 'object',
                'additionalProperties': true,
                'required': [
                    'UpperLimit',
                ],
                'properties': {
                    'UpperLimit': {
                        'type': 'number',
                        'format': 'double',
                    },                            
                }
            }";

            var data = @"{
                'UpperLimit': 1.1111111111111111E+101
            }";

            var validationSchema = JsonSchema4.FromJsonAsync(json).Result;

            /// Act
            var errors = validationSchema.Validate(data);

            /// Assert
            Assert.AreEqual(0, errors.Count);
        }
    }
}
