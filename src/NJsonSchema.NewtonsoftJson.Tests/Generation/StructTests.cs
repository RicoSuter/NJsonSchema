using Newtonsoft.Json;
using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.NewtonsoftJson.Tests.Generation
{
    public class StructTests
    {
        [JsonSchema("UserDefinedStruct")]
        public struct UserDefinedStruct
        {
        }

        public class UserDefinedClass
        {
            [JsonProperty]
            public readonly UserDefinedStruct NonNullableField;

            [JsonProperty]
            public readonly UserDefinedStruct? NullableField;
        }

        [Fact]
        public void Should_have_a_shared_struct_schema()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<UserDefinedClass>();
            var data = schema.ToJson();

            // Assert
            Assert.Single(schema.Definitions);
        }
    }
}