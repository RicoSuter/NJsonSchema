using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class InterfaceTests
    {
        public class BusinessCategory : ICategory
        {
            public string Key { get; set; }

            public string DisplayName { get; set; }

            public IEnumerable<BusinessCategory> Children { get; set; }

            public IEnumerable<ICategory> Elements { get; set; }
        }

        public interface ICategory
        {
            string DisplayName { get; set; }

            string Key { get; set; }
        }

        [TestMethod]
        public async Task When_class_inherits_from_interface_then_properties_for_interface_are_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<BusinessCategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true
            });

            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(2, schema.Definitions["ICategory"].Properties.Count);
        }
    }
}
