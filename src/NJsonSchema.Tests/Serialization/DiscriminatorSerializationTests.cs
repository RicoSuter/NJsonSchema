using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Serialization
{
    public class DiscriminatorSerializationTests
    {
        [Fact]
        public async Task When_discriminator_object_is_set_then_schema_is_correctly_serialized()
        {
            // Arrange
            var childSchema = new JsonSchema
            {
                Type = JsonObjectType.Object,
            };

            var schema = new JsonSchema();
            schema.Definitions["Foo"] = childSchema;
            schema.DiscriminatorObject = new OpenApiDiscriminator
            {
                PropertyName = "discr",
                Mapping =
                {
                    {
                        "Bar",
                        new JsonSchema
                        {
                            Reference = childSchema
                        }
                    }
                }
            };

            // Act
            var json = schema.ToJson();
            var schema2 = await JsonSchema.FromJsonAsync(json);
            var json2 = schema2.ToJson();

            // Assert
            await VerifyHelper.Verify(json);

            Assert.Equal(json, json2);

            Assert.Equal(schema2.Definitions["Foo"], schema2.ActualDiscriminatorObject.Mapping["Bar"].ActualSchema);
        }

        [Fact]
        public void When_discriminator_is_set_then_discriminator_object_is_created()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Discriminator = "discr";

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.Contains(@"""discr""", json);
        }

        [Fact]
        public void When_schema_is_serialized_for_Swagger_then_discriminator_is_string()
        {
            // Arrange
            var childSchema = new JsonSchema
            {
                Type = JsonObjectType.Object,
            };

            var schema = new JsonSchema();
            schema.Definitions["Foo"] = childSchema;
            schema.DiscriminatorObject = new OpenApiDiscriminator
            {
                PropertyName = "discr",
                Mapping =
                {
                    {
                        "Bar",
                        new JsonSchema
                        {
                            Reference = childSchema
                        }
                    }
                }
            };

            // Act
            var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, new DefaultContractResolver(), Formatting.Indented);

            // Assert
            Assert.Contains(@"""discriminator"": ""discr""", json);
            Assert.DoesNotContain(@"""Bar"": ""#/definitions/Foo""", json);
        }
    }
}
