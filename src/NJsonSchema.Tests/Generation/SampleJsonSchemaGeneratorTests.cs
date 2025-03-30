﻿using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class SampleJsonSchemaGeneratorTests
    {
        [Fact]
        public void PrimitiveProperties()
        {
            // Arrange
            var data = @"{
                int: 1, 
                float: 340282346638528859811704183484516925440.0,
                str: ""abc"", 
                bool: true, 
                date: ""2012-07-19"", 
                datetime: ""2012-07-19 10:11:11"", 
                timespan: ""10:11:11""
            }";
            var generator = new SampleJsonSchemaGenerator();

            // Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["int"].Type);
            Assert.Equal(JsonObjectType.String, schema.Properties["str"].Type);
            Assert.Equal(JsonObjectType.Boolean, schema.Properties["bool"].Type);
            Assert.Equal(JsonObjectType.Number, schema.Properties["float"].Type);

            Assert.Equal(JsonObjectType.String, schema.Properties["date"].Type);
            Assert.Equal(JsonFormatStrings.Date, schema.Properties["date"].Format);

            Assert.Equal(JsonObjectType.String, schema.Properties["datetime"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["datetime"].Format);

            Assert.Equal(JsonObjectType.String, schema.Properties["timespan"].Type);
            Assert.Equal(JsonFormatStrings.Duration, schema.Properties["timespan"].Format);
        }

        [Fact]
        public void OpenApi3Properties()
        {
            // Arrange
            var data = @"{
                int: 12345, 
                long: 1736347656630,
                float: 340282346638528859811704183484516925440.0,
                double: 340282346638528859811704183484516925440123456.0,
            }";
            var generator = new SampleJsonSchemaGenerator(new SampleJsonSchemaGeneratorSettings {SchemaType = SchemaType.OpenApi3});

            // Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["int"].Type);
            Assert.Equal(JsonFormatStrings.Integer, schema.Properties["int"].Format);

            Assert.Equal(JsonObjectType.Integer, schema.Properties["long"].Type);
            Assert.Equal(JsonFormatStrings.Long, schema.Properties["long"].Format);

            Assert.Equal(JsonObjectType.Number, schema.Properties["float"].Type);
            Assert.Equal(JsonFormatStrings.Float, schema.Properties["float"].Format);

            Assert.Equal(JsonObjectType.Number, schema.Properties["double"].Type);
            Assert.Equal(JsonFormatStrings.Double, schema.Properties["double"].Format);
        }

        [Fact]
        public void ComplexArrayProperty()
        {
            // Arrange
            var data = @"{
                persons: [
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

            // Act
            var schema = JsonSchema.FromSampleJson(data);
            var json = schema.ToJson();
            var property = schema.Properties["persons"].ActualTypeSchema;

            // Assert
            Assert.Equal(JsonObjectType.Array, property.Type);
            Assert.Equal(3, property.Item.ActualSchema.Properties.Count);
            Assert.True(schema.Definitions.ContainsKey("Person"));
        }

        [Fact]
        public void MergedSchemas()
        {
            // Arrange
            var data = @"{
    ""Address"": {
        ""Street"": [
            {
                ""Street"": ""Straße 1"",
                ""House"": {
                    ""Floor"": ""1"",
                    ""Number"": ""35""
                }
},
            {
                ""Street"": ""Straße 2"",
                ""House"": {
                    ""Floor"": ""2"",
                    ""Number"": ""54""
                }
            }
        ],
        ""@first_name"": ""Albert"",
        ""@last_name"": ""Einstein""
    }
}";

            // Act
            var schema = JsonSchema.FromSampleJson(data);
            var json = schema.ToJson();

            // Assert
            Assert.Equal(3, schema.Definitions.Count);
            Assert.True(schema.Definitions.ContainsKey("Street"));
            Assert.True(schema.Definitions.ContainsKey("House"));
            Assert.True(schema.Definitions.ContainsKey("Address"));
        }

        [Fact]
        public void PrimitiveArrayProperty()
        {
            // Arrange
            var data = @"{
                array: [ 1, true ]
            }";
            var generator = new SampleJsonSchemaGenerator();

            // Act
            var schema = generator.Generate(data);
            var json = schema.ToJson();
            var property = schema.Properties["array"].ActualTypeSchema;

            // Assert
            Assert.Equal(JsonObjectType.Array, property.Type);
            Assert.Equal(JsonObjectType.Integer, property.Item.ActualSchema.Type);
        }
    }
}
