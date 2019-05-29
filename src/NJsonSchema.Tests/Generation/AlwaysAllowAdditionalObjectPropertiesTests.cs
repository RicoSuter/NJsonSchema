using System.Linq;
using NJsonSchema.Generation;
using System.Collections.Generic;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AlwaysAllowAdditionalObjectPropertiesTests
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
        public void AlwaysAllowAdditionalObjectProperties_true()
        {
            var schema = JsonSchema.FromType<Employee>(new JsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });

            Assert.True(schema.AllOf.First().AllowAdditionalProperties);
            Assert.True(schema.Definitions.First().Value.AllowAdditionalProperties);
        }


        [Fact]
        public void DictionarySchemasAreEquivalent()
        {
            var schemaTrue = JsonSchema.FromType<Dictionary<string, string>>(new JsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = true
            });

            var schemaFalse = JsonSchema.FromType<Dictionary<string, string>>(new JsonSchemaGeneratorSettings
            {
                AlwaysAllowAdditionalObjectProperties = false
            });

            Assert.NotNull(schemaTrue.AdditionalPropertiesSchema);
            Assert.Equal(schemaTrue.AdditionalPropertiesSchema.Type, schemaFalse.AdditionalPropertiesSchema.Type);
        }
    }
}
