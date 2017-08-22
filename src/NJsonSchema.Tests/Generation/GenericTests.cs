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
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.Array, schema.Type);
            Assert.AreEqual(JsonObjectType.String, schema.Item.Type);
        }

        public class A<T>
        {
            public T B { get; set; }
        }

        [TestMethod]
        public async Task When_open_generic_type_is_generated_then_no_exception_is_thrown()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync(typeof(A<>));

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsNotNull(json);
        }
    }
}
