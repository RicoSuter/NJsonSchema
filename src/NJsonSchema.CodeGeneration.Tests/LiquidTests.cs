using DotLiquid;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests
{
    public class LiquidTests
    {
        public class TestModel
        {
            public string Foo { get; } = "Foo.";

            public Dictionary<string, object> Bar { get; } = new Dictionary<string, object>();
        }

        [Fact]
        public void LiquidModelHasDictionry_KeyAccessShouldWork()
        {
            /// Arrange
            var model = new TestModel();
            model.Bar["Baz"] = "abc";

            /// Act
            var hash = new LiquidProxyHash(model);
            var template = Template.Parse("Hi {{ Foo }} {{ Bar[\"Baz\"] }}");
            var text = template.Render(new RenderParameters(CultureInfo.InvariantCulture)
            {
                LocalVariables = hash,
            });

            /// Assert
            Assert.Equal("Hi Foo. abc", text);
        }
    }
}
