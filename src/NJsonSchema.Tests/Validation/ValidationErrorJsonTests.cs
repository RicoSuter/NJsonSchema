using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class ValidationErrorJsonTests
    {
        [Fact]
        public void When_all_of_property_is_missing_then_nested_errors_are_converted_to_json()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.AllOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Object,
                RequiredProperties = { "someproperty" }
            });
            schema.AllOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Object
            });

            var token = new JObject();

            // Act
            var errors = schema.Validate(token);
            var json = ValidationError.ToJArray(errors);

            // Assert
            var error = Assert.IsType<JObject>(Assert.Single(json));
            Assert.Equal("NotAllOf", error["kind"]!.Value<string>());

            var subschemaErrors = Assert.IsType<JArray>(error["errors"]);
            Assert.Equal(2, subschemaErrors.Count);

            var childError = Assert.IsType<JObject>(Assert.Single(subschemaErrors[0]));
            Assert.Equal("PropertyRequired", childError["kind"]!.Value<string>());
            Assert.Equal("#/someproperty", childError["path"]!.Value<string>());
            Assert.Equal("someproperty", childError["property"]!.Value<string>());

            Assert.Empty(subschemaErrors[1]);
        }

        [Fact]
        public void When_no_type_validates_then_errors_are_grouped_by_type()
        {
            // Arrange
            var schema = new JsonSchema
            {
                Type = JsonObjectType.String | JsonObjectType.Integer
            };

            var token = new JValue(1.5);

            // Act
            var errors = schema.Validate(token);
            var json = Assert.Single(errors).ToJObject();

            // Assert
            Assert.Equal("NoTypeValidates", json["kind"]!.Value<string>());

            var typeErrors = Assert.IsType<JObject>(json["errors"]);
            Assert.Equal("StringExpected", typeErrors["String"]![0]!["kind"]!.Value<string>());
            Assert.Equal("IntegerExpected", typeErrors["Integer"]![0]!["kind"]!.Value<string>());
        }

        [Fact]
        public void When_error_has_no_line_info_then_line_properties_are_omitted()
        {
            // Arrange
            var schema = new JsonSchema { Type = JsonObjectType.String };

            // Act
            var json = Assert.Single(schema.Validate(new JValue(5))).ToJObject();

            // Assert
            Assert.Equal("StringExpected", json["kind"]!.Value<string>());
            Assert.Null(json["lineNumber"]);
            Assert.Null(json["linePosition"]);
            Assert.Null(json["errors"]);
        }
    }
}
