using Xunit;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.Tests.Generation
{
    public class AllowAdditionalPropertiesTests
    {
        public class Person
        {
            public string Name { get; set; }
        }

        public class Employee : Person
        {
            public int Salary { get; set; }
        }

        [Fact]
        public void When_AlwaysAllowAdditionalObjectProperties_is_set_then_AllowAdditionalProperties_is_true()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Employee>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });

            // Assert
            Assert.True(schema.AllOf.First().AllowAdditionalProperties);
            Assert.True(schema.Definitions.First().Value.AllowAdditionalProperties);
        }

        [Fact]
        public void When_AlwaysAllowAdditionalObjectProperties_is_used_then_AdditionalPropertiesSchema_is_ok()
        {
            // Act
            var schemaTrue = NewtonsoftJsonSchemaGenerator.FromType<Dictionary<string, string>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });

            var schemaFalse = NewtonsoftJsonSchemaGenerator.FromType<Dictionary<string, string>>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = false
            });

            // Assert
            Assert.NotNull(schemaTrue.AdditionalPropertiesSchema);
            Assert.Equal(schemaTrue.AdditionalPropertiesSchema.Type, schemaFalse.AdditionalPropertiesSchema.Type);
        }
    }
}
