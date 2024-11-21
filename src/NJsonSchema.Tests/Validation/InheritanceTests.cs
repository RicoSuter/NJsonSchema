using Newtonsoft.Json.Linq;
using NJsonSchema.Validation;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class InheritanceTests
    {
        [Fact]
        public void When_is_any_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void When_is_not_any_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(1.5);
            
            //// Act
            var errors = schema.Validate(token);

            //// Assert
            var error = (ChildSchemaValidationError)errors.First();
            Assert.Equal(ValidationErrorKind.NotAnyOf, error.Kind);
            Assert.Same(schema, error.Schema);
            Assert.Equal(ValidationErrorKind.StringExpected, error.Errors.First().Value.First().Kind);
            Assert.Same(schema.AnyOf.First(), error.Errors.First().Value.First().Schema);
        }
        
        [Fact]
        public void When_is_all_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.AnyOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });

            var token = new JValue("Foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void When_is_not_all_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.AllOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.AllOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.NotAllOf, errors.First().Kind);
            Assert.Same(schema, errors.First().Schema);
        }

        [Fact]
        public void When_is_one_of_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(10);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void When_is_not_one_of_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Boolean
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.NotOneOf, errors.First().Kind);
            Assert.Same(schema, errors.First().Schema);
        }

        [Fact]
        public void When_one_of_matches_multiple_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.String
            });
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });
            schema.OneOf.Add(new JsonSchema
            {
                Type = JsonObjectType.Integer
            });

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.NotOneOf, errors.First().Kind);
            Assert.Same(schema, errors.First().Schema);
        }

        [Fact]
        public void When_matches_excluded_schema_then_it_should_fail()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Not = new JsonSchema
            {
                Type = JsonObjectType.String
            };

            var token = new JValue("Foo");

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(ValidationErrorKind.ExcludedSchemaValidates, errors.First().Kind);
            Assert.Same(schema, errors.First().Schema);
        }

        [Fact]
        public void When_not_matches_excluded_schema_then_it_should_succeed()
        {
            //// Arrange
            var schema = new JsonSchema();
            schema.Not = new JsonSchema
            {
                Type = JsonObjectType.String
            };

            var token = new JValue(5);

            //// Act
            var errors = schema.Validate(token);

            //// Assert
            Assert.Equal(0, errors.Count);
        }
    }
}
