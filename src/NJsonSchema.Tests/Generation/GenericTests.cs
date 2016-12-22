using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class GenericTests
    {
        public class Smth : IEnumerable<string>
        {
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<string> GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public async Task When_class_inherits_from_IEnumerable_then_it_should_become_a_json_array_type()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Smth>();

            //// Act
            var json = await schema.ToJsonAsync();

            //// Assert
            Assert.AreEqual(JsonObjectType.Array, schema.Type);
            Assert.AreEqual(JsonObjectType.String, schema.Item.Type);
        }
    }
}
