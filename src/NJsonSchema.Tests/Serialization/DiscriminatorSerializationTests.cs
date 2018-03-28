using Newtonsoft.Json;
using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Serialization
{
    public class DiscriminatorSerializationTests
    {
        [Fact]
        public void When_discriminator_object_is_set_then_schema_is_correctly_serialized()
        {
            //// Arrange
            var childSchema = new JsonSchema4
            {
                Type = JsonObjectType.Object,
            };

            var schema = new JsonSchema4();
            schema.Definitions["Foo"] = childSchema;
            schema.DiscriminatorObject = new OpenApiDiscriminator
            {
                PropertyName = "discr",
                Mapping =
                {
                    {
                        "Bar",
                        new JsonSchema4
                        {
                            Reference = childSchema
                        }
                    }
                }
            };

            //// Act
            JsonSchemaSerializationContext.CurrentSchemaType = SchemaType.Swagger2; // will be ignored
            var json = schema.ToJson();

            //// Assert
            Assert.Contains(@"""propertyName"": ""discr""", json);
            Assert.Contains(@"""Bar""", json);
            Assert.Contains(@"""$ref"": ""#/definitions/Foo""", json);
        }

        [Fact]
        public void When_discriminator_is_set_then_discriminator_object_is_created()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.Discriminator = "discr";

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.Contains(@"""discr""", json);
        }

        [Fact]
        public void When_schema_is_serialized_for_Swagger_then_discriminator_is_string()
        {
            //// Arrange
            var childSchema = new JsonSchema4
            {
                Type = JsonObjectType.Object,
            };

            var schema = new JsonSchema4();
            schema.Definitions["Foo"] = childSchema;
            schema.DiscriminatorObject = new OpenApiDiscriminator
            {
                PropertyName = "discr",
                Mapping =
                {
                    {
                        "Bar",
                        new JsonSchema4
                        {
                            Reference = childSchema
                        }
                    }
                }
            };

            //// Act
            JsonSchemaSerializationContext.CurrentSchemaType = SchemaType.Swagger2;
            var json = JsonConvert.SerializeObject(schema, Formatting.Indented);

            //// Assert
            Assert.Contains(@"""discriminator"": ""discr""", json);
            Assert.DoesNotContain(@"""Bar""", json);
            Assert.DoesNotContain(@"""$ref"": ""#/definitions/Foo""", json);
        }
    }
}
