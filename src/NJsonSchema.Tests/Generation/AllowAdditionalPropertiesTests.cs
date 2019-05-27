using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

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

#if !NET45
        [Fact]
#endif
        public void AllowAdditionalProperties_true()
        {
            var schema = JsonSchema.FromType<Employee>(new JsonSchemaGeneratorSettings
            {
                AllowAdditionalProperties = true
            });

            Assert.True(schema.AllOf.First().AllowAdditionalProperties);
            Assert.True(schema.Definitions.First().Value.AllowAdditionalProperties);
        }
    }
}
