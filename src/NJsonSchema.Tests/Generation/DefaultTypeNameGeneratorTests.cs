using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{   
    [TestClass]
    public class DefaultTypeNameGeneratorTests
    {
        [TestMethod]
        public void When_type_name_is_genererated_then_it_corret()
        {
            var tests = new Dictionary<string, string>
            {
                { "foo.bar", "Bar" },
                { "fooBar", "FooBar" },
                { "abc.fooBar", "FooBar" },
                { "foo[bar]", "FooOfBar" },
                { "bar[a,b]", "BarOfAAndB" },
                { "foo.bar[a.b]", "BarOfB" },
                { "foo.bar[a.b, c.d]", "BarOfBAndD" },
                { " Foo.ActionResponse [System.Net.HttpStatusCode, User]", "ActionResponseOfHttpStatusCodeAndUser" }
            };

            //// Arrange
            var generator = new DefaultTypeNameGenerator();

            foreach (var p in tests)
            {
                //// Act
                var name = generator.Generate(new JsonSchema4(), p.Key, new List<string>());

                //// Assert
                Assert.AreEqual(p.Value, name);
            }
        }
    }
}
