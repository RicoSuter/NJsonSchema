using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class SampleJsonSchemaGeneratorTests
    {
        [Fact]
        public void PrimitiveProperties()
        {
            //// Arrange
            var data = @"{
                int: 1, 
                str: ""abc"", 
                bool: true, 
                date: ""2012-07-19"", 
                datetime: ""2012-07-19 10:11:11"", 
                timespan: ""10:11:11""
            }";
            var generator = new SampleJsonSchemaGenerator();

            //// Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["int"].Type);
            Assert.Equal(JsonObjectType.String, schema.Properties["str"].Type);
            Assert.Equal(JsonObjectType.Boolean, schema.Properties["bool"].Type);

            Assert.Equal(JsonObjectType.String, schema.Properties["date"].Type);
            Assert.Equal(JsonFormatStrings.Date, schema.Properties["date"].Format);

            Assert.Equal(JsonObjectType.String, schema.Properties["datetime"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["datetime"].Format);

            Assert.Equal(JsonObjectType.String, schema.Properties["timespan"].Type);
            Assert.Equal(JsonFormatStrings.TimeSpan, schema.Properties["timespan"].Format);
        }

        [Fact]
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
            var schema = JsonSchema4.FromSampleJson(data);
            var json = schema.ToJson();
            var property = schema.Properties["array"].ActualTypeSchema;

            //// Assert
            Assert.Equal(JsonObjectType.Array, property.Type);
            Assert.Equal(3, property.Item.ActualSchema.Properties.Count);
        }

        [Fact]
        public void PrimitiveArrayProperty()
        {
            //// Arrange
            var data = @"{
                array: [ 1, true ]
            }";
            var generator = new SampleJsonSchemaGenerator();

            //// Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();
            var property = schema.Properties["array"].ActualTypeSchema;

            //// Assert
            Assert.Equal(JsonObjectType.Array, property.Type);
            Assert.Equal(JsonObjectType.Integer, property.Item.ActualSchema.Type);
        }
    }
}
