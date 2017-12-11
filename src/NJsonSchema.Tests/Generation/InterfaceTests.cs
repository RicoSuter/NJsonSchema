using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
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
            Assert.Equal(2, schema.Definitions["ICategory"].Properties.Count);
        }
    }
}
