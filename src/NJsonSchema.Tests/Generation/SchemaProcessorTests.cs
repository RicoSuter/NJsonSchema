using System.Threading.Tasks;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class SchemaProcessorTests
    {
        public class MyTestSchemaProcessor : ISchemaProcessor
        {
            private readonly object _example;

            public MyTestSchemaProcessor(object example)
            {
                _example = example;
            }

            public Task ProcessAsync(SchemaProcessorContext context)
            {
                context.Schema.Example = _example;
                return Task.FromResult<object>(null);
            }
        }

        [JsonSchemaProcessor(typeof(MyTestSchemaProcessor), "example123")]
        public class ClassWithSchemaProcessor
        {
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_class_has_schema_processor_attribute_then_it_is_processed()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ClassWithSchemaProcessor>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var data = schema.ToJson();

            //// Assert
            Assert.Equal("example123", schema.Example);
        }
    }
}
