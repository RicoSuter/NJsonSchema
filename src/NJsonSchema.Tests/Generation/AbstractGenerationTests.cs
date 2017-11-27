using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class AbstractGenerationTests
    {
        public abstract class AbstractClass
        {
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_class_is_abstract_then_is_abstract_is_true()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<AbstractClass>();
            var json = schema.ToJson();

            /// Assert
            Assert.IsTrue(json.Contains("x-abstract"));
            Assert.IsTrue(schema.IsAbstract);
        }
        
        public class NotAbstractClass
        {
            public string Foo { get; set; }
        }
        
        [TestMethod]
        public async Task When_class_is_not_abstract_then_is_abstract_is_false()
        {
            /// Act
            var schema = await JsonSchema4.FromTypeAsync<NotAbstractClass>();
            var json = schema.ToJson();

            /// Assert
            Assert.IsFalse(json.Contains("x-abstract"));
            Assert.IsFalse(schema.IsAbstract);
        }
    }
}
