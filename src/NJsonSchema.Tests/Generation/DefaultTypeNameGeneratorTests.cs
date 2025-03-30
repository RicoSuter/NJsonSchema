using Xunit;

namespace NJsonSchema.Tests.Generation
{   
    public class DefaultTypeNameGeneratorTests
    {
        [Fact]
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

            // Arrange
            var generator = new DefaultTypeNameGenerator();

            foreach (var p in tests)
            {
                // Act
                var name = generator.Generate(new JsonSchema(), p.Key, new List<string>());

                // Assert
                Assert.Equal(p.Value, name);
            }
        }

        [Fact]
        public void When_type_name_is_genererated_then_it_removes_Illegal_characters()
        {
            var tests = new Dictionary<string, string>
            {
                { "foo.bar+", "Bar" },
                { "foo+Bar", "Foo_Bar" },
                { "foo++Bar", "Foo_Bar" },
                { "abc.1fooBar", "_1fooBar" },
                { "foo9[bar]", "Foo9OfBar" },
                { "bar[a+,b]", "BarOfA_AndB" },
                { "foo.bar[a.b+]", "BarOfB" },
                { "foo.bar[a.b+, c.+d+]", "BarOfB_And_d" },
                { " Foo.ActionRes+ponse [System.Net.HttpSta+tusCode, Us+er]", "ActionRes_ponseOfHttpSta_tusCodeAndUs_er" }
            };

            // Arrange
            var generator = new DefaultTypeNameGenerator();

            foreach (var p in tests)
            {
                // Act
                var name = generator.Generate(new JsonSchema(), p.Key, new List<string>());

                // Assert
                Assert.Equal(p.Value, name);
            }
        }
    }
}
