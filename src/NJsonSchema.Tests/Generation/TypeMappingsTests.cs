using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class TypeMappingsTests
    {
        public class Foo
        {
            public Bar Bar { get; set; }
        }

        public class Bar
        {

        }

        [TestMethod]
        public void When_type_mapping_is_available_for_type_then_it_is_called()
        {
            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                TypeMappings =
                {
                    {typeof(Bar), (s, schemaGenerator) =>
                        {
                            s.Type = JsonObjectType.String;
                        }
                    }
                }
            });

            //// Assert
            var property = schema.Properties["Bar"].ActualSchema;
            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.String));
        }
    }
}
