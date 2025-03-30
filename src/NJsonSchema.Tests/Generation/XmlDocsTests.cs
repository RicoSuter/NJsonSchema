using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class XmlDocsTests
    {
        /// <summary>Foobar.</summary>
        /// <example>
        /// { "foo": "bar" }
        /// </example>
        public abstract class AbstractClass
        {
            /// <example>
            /// { "abc": "def" }
            /// </example>
            public string Foo { get; set; }

            /// <example>Bar.</example>
            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_example_xml_docs_is_defined_then_examples_can_be_defined()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AbstractClass>();
            var json = schema.ToJson(Formatting.None);

            // Assert
            Assert.Contains(@"Foobar.", json);
            Assert.Contains(@"""x-example"":{""foo"":""bar""}", json);
            Assert.Contains(@"""x-example"":{""abc"":""def""}", json);
            Assert.Contains(@"""x-example"":""Bar.""", json);
        }
    }
}
