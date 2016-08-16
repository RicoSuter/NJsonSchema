using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class TypeMappingsTests
    {
        public class Foo
        {
            public Bar Bar1 { get; set; }

            public Bar Bar2 { get; set; }
        }

        public class Bar
        {

        }

        [TestMethod]
        public void When_primitive_type_mapping_is_available_for_type_then_it_is_called()
        {
            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                TypeMappers =
                {
                    new PrimitiveTypeMapper(typeof(Bar), s => s.Type = JsonObjectType.String)
                }
            });

            //// Assert
            var json = schema.ToJson();
            var property = schema.Properties["Bar1"].ActualPropertySchema;

            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.String));
            Assert.IsFalse(json.Contains("$ref"));
        }

        [TestMethod]
        public void When_object_type_mapping_is_available_for_type_then_it_is_called()
        {
            //// Act
            var schema = JsonSchema4.FromType<Foo>(new JsonSchemaGeneratorSettings
            {
                TypeMappers =
                {
                    new ObjectTypeMapper(typeof(Bar), new JsonSchema4
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {
                                    "Prop",
                                    new JsonProperty
                                    {
                                        IsRequired = true,
                                        Type = JsonObjectType.String
                                    }
                                }
                            }
                        }
                    )
                }
            });

            //// Assert
            var json = schema.ToJson();

            var property1 = schema.Properties["Bar1"];
            var property2 = schema.Properties["Bar2"];

            Assert.IsTrue(property1.ActualPropertySchema.Properties.ContainsKey("Prop"));
            Assert.IsTrue(property1.ActualPropertySchema == property2.ActualPropertySchema);

            Assert.IsTrue(json.Contains("$ref"));
        }
    }
}
