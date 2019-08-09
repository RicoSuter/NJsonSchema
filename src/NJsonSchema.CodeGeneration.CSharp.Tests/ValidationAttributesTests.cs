using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class ValidationAttributesTests
    {
        [Fact]
        public async Task When_string_property_has_maxlength_then_stringlength_attribute_is_rendered_in_Swagger_mode()
        {
            //// Arrange
            const string json = @"{
                        'type': 'object',
                        'required': [ 'value' ],
                        'properties': {
                            'value': {
                                '$ref': '#/definitions/string50'
                            }
                        },
                'definitions': {
                    'string50': {
                        'type': 'string',
                        'maxLength': 50
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            //// Assert
            Assert.Null(schema.Properties["value"].MaxLength);
            Assert.Equal(50, schema.Properties["value"].ActualSchema.MaxLength);

            Assert.Contains("[System.ComponentModel.DataAnnotations.StringLength(50)]\n" +
                            "        public string Value { get; set; }\n", code);
        }
    }
}
