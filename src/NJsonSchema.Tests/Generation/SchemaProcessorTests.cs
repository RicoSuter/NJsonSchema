using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

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

            public void Process(SchemaProcessorContext context)
            {
                context.Schema.Example = _example;
            }
        }

        [JsonSchemaProcessor(typeof(MyTestSchemaProcessor), "example123")]
        public class ClassWithSchemaProcessor
        {
            public string Foo { get; set; }
        }

        [Fact]
        public void When_class_has_schema_processor_attribute_then_it_is_processed()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithSchemaProcessor>(new NewtonsoftJsonSchemaGeneratorSettings());
            var data = schema.ToJson();

            // Assert
            Assert.Equal("example123", schema.Example);
        }
    }
}
