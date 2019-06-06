using NJsonSchema.Generation;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class IgnoreDuplicateInheritanceHierarchyTests
    {
        // Arrange data
        public class Map
        {
            public int LocA { get; set; }
            public int LocB { get; set; }
        }

        public class Person
        {
            public List<string> Maps { get; set; }
        }

        public class Teacher : Person
        {
            public List<Map> Maps { get; set; }
        }


        [Fact]
        public async Task When_IgnoreDuplicateIbheritanceHierarchy_is_enabled_then_ignore_base_property_with_same_name()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true,
                IgnoreInhertianceDuplicates = true
            };

            //// Act & Assertion
            try
            {
                var schema = JsonSchema.FromType(typeof(Teacher), settings);
                var data = schema.ToJson();

                // if there's a definition it correctly parsed the data
                Assert.True(schema.Definitions.ContainsKey("Map"));

                // if no errors that's good
                Assert.True(true);
            }
            catch
            {
                // if any errors we failed
                Assert.False(true);
            }

        }
    }
}
