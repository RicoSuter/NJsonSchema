using Newtonsoft.Json;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class XmlDocsTests
    {
        /// <example>
        /// { "foo": "bar" }
        /// </example>
        public abstract class AbstractClass
        {
            /// <example>
            /// { "abc": "def" }
            /// </example>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_example_xml_docs_is_defined_then_examples_can_be_defined()
        {
            /// Act
            var schema = JsonSchema.FromType<AbstractClass>();
            var json = schema.ToJson(Formatting.None);

            /// Assert
            Assert.Contains(@"""x-example"":{""foo"":""bar""}", json);
            Assert.Contains(@"""x-example"":{""abc"":""def""}", json);
        }
    }
}
