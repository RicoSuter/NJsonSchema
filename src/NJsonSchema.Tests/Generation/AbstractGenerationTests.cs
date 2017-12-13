using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AbstractGenerationTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_class_is_abstract_then_is_abstract_is_true()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<AbstractClass>();
            var json = schema.ToJson();

            /// Assert
            Assert.Contains("x-abstract", json);
            Assert.True(schema.IsAbstract);
        }
        
        public class NotAbstractClass
        {
            public string Foo { get; set; }
        }
        
        [Fact]
        public async Task When_class_is_not_abstract_then_is_abstract_is_false()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<NotAbstractClass>();
            var json = schema.ToJson();

            /// Assert
            Assert.DoesNotContain("x-abstract", json);
            Assert.False(schema.IsAbstract);
        }
    }
}
