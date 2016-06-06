using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class FlattenInheritanceHierarchyTests
    {
        public class Person
        {
            public string Name { get; set; }
        }

        public class Teacher : Person
        {
            public string Class { get; set; }
        }

        [TestMethod]
        public void When_FlattenInheritanceHierarchy_is_enabled_then_all_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = JsonSchema4.FromType(typeof(Teacher), settings);
            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("Name"));
            Assert.IsTrue(schema.Properties.ContainsKey("Class"));
        }
    }
}
