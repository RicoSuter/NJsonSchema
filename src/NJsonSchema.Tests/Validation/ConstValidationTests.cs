using NJsonSchema.Validation;

namespace NJsonSchema.Tests.Validation
{
    public class ConstValidationTests
    {
        [Fact]
        public async Task When_const_is_defined_and_value_matches_then_validation_succeeds()
        {
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""type"": {
                        ""const"": ""System.Type""
                    }
                },
                ""required"": [""type""]
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate(@"{ ""type"": ""System.Type"" }");

            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_defined_and_value_does_not_match_then_validation_fails()
        {
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""type"": {
                        ""const"": ""System.Type""
                    }
                },
                ""required"": [""type""]
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate(@"{ ""type"": ""test"" }");

            Assert.Single(errors);
        }

        [Fact]
        public async Task When_const_is_integer_and_value_matches_then_validation_succeeds()
        {
            var json = @"{ ""const"": 42 }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate("42");

            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_integer_and_value_does_not_match_then_validation_fails()
        {
            var json = @"{ ""const"": 42 }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate("99");

            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.ConstMismatch, errors.First().Kind);
        }

        [Fact]
        public async Task When_const_is_boolean_then_validation_works()
        {
            var json = @"{ ""const"": true }";
            var schema = await JsonSchema.FromJsonAsync(json);

            Assert.Empty(schema.Validate("true"));
            Assert.Single(schema.Validate("false"));
        }

        [Fact]
        public async Task When_const_is_null_then_validation_works()
        {
            var json = @"{ ""const"": null }";
            var schema = await JsonSchema.FromJsonAsync(json);

            Assert.Empty(schema.Validate("null"));

            var errors = schema.Validate(@"""something""");
            Assert.Single(errors);
            Assert.Equal(ValidationErrorKind.ConstMismatch, errors.First().Kind);
        }

        [Fact]
        public async Task When_const_is_object_then_validation_works()
        {
            var json = @"{
                ""const"": { ""key"": ""value"" }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            Assert.Empty(schema.Validate(@"{ ""key"": ""value"" }"));
            Assert.Single(schema.Validate(@"{ ""key"": ""wrong"" }"));
        }

        [Fact]
        public async Task When_no_const_is_defined_then_validation_passes()
        {
            var json = @"{ ""type"": ""string"" }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate(@"""anything""");

            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_const_is_set_then_schema_roundtrips()
        {
            var json = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""type"": {
                        ""const"": ""System.Type""
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var output = schema.ToJson();
            var schema2 = await JsonSchema.FromJsonAsync(output);

            Assert.True(schema2.Properties["type"].HasConst);
            Assert.Equal("System.Type", schema2.Properties["type"].Const?.ToString());
        }

        [Fact]
        public void When_const_is_not_set_then_HasConst_is_false()
        {
            var schema = new JsonSchema { Type = JsonObjectType.String };

            Assert.False(schema.HasConst);
            Assert.Null(schema.Const);
        }

        [Fact]
        public async Task When_const_with_additional_properties_false_then_wrong_value_fails()
        {
            var json = @"{
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""type"": ""object"",
                ""properties"": {
                    ""type"": {
                        ""const"": ""System.Type""
                    }
                },
                ""additionalProperties"": false,
                ""required"": [""type""]
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            var errors = schema.Validate(@"{ ""type"": ""test"" }");

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Kind == ValidationErrorKind.ConstMismatch);
        }
    }
}
