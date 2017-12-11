using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
        public async Task When_class_inherits_from_IEnumerable_then_it_should_become_a_json_array_type()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Smth>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Array, schema.Type);
            Assert.Equal(JsonObjectType.String, schema.Item.Type);
        }

        public class A<T>
        {
            public T B { get; set; }
        }

        [Fact]
        public async Task When_open_generic_type_is_generated_then_no_exception_is_thrown()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync(typeof(A<>));

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.NotNull(json);
        }
    }
}
