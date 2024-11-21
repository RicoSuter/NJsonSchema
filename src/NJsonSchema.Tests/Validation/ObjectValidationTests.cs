using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class ObjectValidationTests
    {
        [Fact]
        public void When_token_is_not_object_then_validation_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty();

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.ObjectExpected, errors.First().Kind);
            Assert.Equal("10", errors.First().Token?.ToString());
        }

        [Fact]
        public void When_required_property_is_missing_then_it_should_be_in_error_list()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                IsRequired = true,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Single(errors);
            Assert.Equal("Foo", errors.First().Property);
            Assert.Equal("#/Foo", errors.First().Path);
            Assert.Equal(ValidationErrorKind.PropertyRequired, errors.First().Kind);
            Assert.Equal("{}", errors.First().Token?.ToString());
        }

        [Fact]
        public void When_property_matches_one_of_the_types_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Number | JsonObjectType.Null
            };

            var token = new JObject();
            token["Foo"] = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_optional_property_is_missing_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                IsRequired = false,
            };

            var token = new JObject();

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_string_property_is_available_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue("Bar");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void When_string_property_required_but_integer_provided_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                IsRequired = true,
                Type = JsonObjectType.String
            };

            var token = new JObject();
            token["Foo"] = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.StringExpected, errors.First().Kind);
            Assert.Equal("Foo", errors.First().Property);
            Assert.Equal("#/Foo", errors.First().Path);
            Assert.Equal("10", errors.First().Token?.ToString());
        }

        [Fact]
        public async Task When_type_property_has_integer_type_then_it_is_validated_correctly()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(
                @"{
              ""$schema"": ""http://json-schema.org/draft-06/schema#"",
              ""type"": ""object"",
              ""additionalProperties"": false,
              ""properties"": {
                ""type"" : {""type"" : ""integer""}
              }
            }");

            //// Act
            var errors = schema.Validate(
                @"{
              ""type"": 1
            }");

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void When_case_sensitive_and_property_has_different_casing_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Number | JsonObjectType.Null
            };

            var token = new JObject();
            token["foo"] = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.NoAdditionalPropertiesAllowed, errors.First().Kind);
            Assert.Equal("\"foo\": 5", errors.First().Token?.ToString());
        }

        [Fact]
        public void When_case_insensitive_and_property_has_different_casing_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.Object;
            schema.AllowAdditionalProperties = false;
            schema.Properties["Foo"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Number | JsonObjectType.Null
            };

            var token = new JObject();
            token["foo"] = new JValue(5);

            var validator = new JsonSchemaValidator(new JsonSchemaValidatorSettings()
            {
                PropertyStringComparer = StringComparer.OrdinalIgnoreCase,
            });

            //// Act
            var errors = validator.Validate(token, schema);

            //// Assert
            Assert.Empty(errors);
        }
    }
}