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
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync(typeof(Teacher), settings);
            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("Name"));
            Assert.IsTrue(schema.Properties.ContainsKey("Class"));
        }

        public interface IFoo : IBar, IBaz
        {
            string Foo { get; set; }
        }

        public interface IBar
        {
            string Bar { get; set; }
        }

        public interface IBaz
        {
            string Baz { get; set; }
        }

        public interface ISame
        {
            string Bar { get; set; }
        }

        [TestMethod]
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_interface_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.String,
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<IFoo>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.AreEqual(3, schema.Properties.Count);
            Assert.IsTrue(schema.Properties.ContainsKey("Foo"));
            Assert.IsTrue(schema.Properties.ContainsKey("Bar"));
            Assert.IsTrue(schema.Properties.ContainsKey("Baz"));
        }
    }
}
