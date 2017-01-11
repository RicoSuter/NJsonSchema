using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class DataToJsonSchemaGeneratorTests
    {
        [TestMethod]
        public void PrimitiveProperties()
        {
            //// Arrange
            var data = @"{
                int: 1, 
                str: ""abc"", 
                bool: true, 
                datetime: ""2012-07-19"", 
                timespan: ""10:11:11"", 
            }";
            var generator = new DataToJsonSchemaGenerator();

            //// Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.Integer, schema.Properties["int"].Type);
            Assert.AreEqual(JsonObjectType.String, schema.Properties["str"].Type);
            Assert.AreEqual(JsonObjectType.Boolean, schema.Properties["bool"].Type);

            //Assert.AreEqual(JsonObjectType.String, schema.Properties["datetime"].Type);
            //Assert.AreEqual(JsonFormatStrings.DateTime, schema.Properties["datetime"].Format);

            //Assert.AreEqual(JsonObjectType.String, schema.Properties["timespan"].Type);
            //Assert.AreEqual(JsonFormatStrings.TimeSpan, schema.Properties["timespan"].Format);
        }

        [TestMethod]
        public void ComplexArrayProperty()
        {
            //// Arrange
            var data = @"{
                array: [
                    {
                        foo: ""bar"", 
                        bar: ""foo""
                    },
                    {
                        foo: ""bar"", 
                        puk: ""fii""
                    }
                ]
            }";

            //// Act
            var schema = JsonSchema4.FromData(data);
            var json = schema.ToJson();
            var property = schema.Properties["array"].ActualPropertySchema;

            //// Assert
            Assert.AreEqual(JsonObjectType.Array, property.Type);
            Assert.AreEqual(3, property.Item.ActualSchema.Properties.Count);
        }

        [TestMethod]
        public void PrimitiveArrayProperty()
        {
            //// Arrange
            var data = @"{
                array: [ 1, true ]
            }";
            var generator = new DataToJsonSchemaGenerator();

            //// Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();
            var property = schema.Properties["array"].ActualPropertySchema;

            //// Assert
            Assert.AreEqual(JsonObjectType.Array, property.Type);
            Assert.AreEqual(JsonObjectType.Integer, property.Item.ActualSchema.Type);
        }
    }
}
