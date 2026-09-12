using System.Globalization;
using System.Text;

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

        [Theory]
        [InlineData("{ // comment\n unquoted: 'value', trailing: 1, }")]
        [InlineData("{ unquoted: 'value', trailing: 1 }")]
        public void StreamInputHasSameLenientSemanticsAsStringInput(string data)
        {
            // Arrange
            var generator = new SampleJsonSchemaGenerator();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            // Act
            var stringSchema = generator.Generate(data);
            var streamSchema = generator.Generate(stream);

            // Assert
            Assert.Equal(JsonObjectType.Object, streamSchema.Type);
            Assert.Equal(JsonObjectType.String, streamSchema.Properties["unquoted"].Type);
            Assert.Equal(JsonObjectType.Integer, streamSchema.Properties["trailing"].Type);
            Assert.Equal(stringSchema.ToJson(), streamSchema.ToJson());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StreamInputSupportsByteOrderMarksAndDisposesStream(bool useUtf16)
        {
            // Arrange
            var encoding = useUtf16 ? Encoding.Unicode : new UTF8Encoding(true);
            var preamble = encoding.GetPreamble();
            var content = encoding.GetBytes("{ value: 'text', }");
            var bytes = preamble.Concat(content).ToArray();
            var stream = new TrackingMemoryStream(bytes);
            var generator = new SampleJsonSchemaGenerator();

            // Act
            var schema = generator.Generate(stream);

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["value"].Type);
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void InvalidStreamInputDisposesStream()
        {
            // Arrange
            var stream = new TrackingMemoryStream(Encoding.UTF8.GetBytes("{"));
            var generator = new SampleJsonSchemaGenerator();

            // Act
            Assert.ThrowsAny<Exception>(() => generator.Generate(stream));

            // Assert
            Assert.True(stream.IsDisposed);
        }

        [Fact]
        public void DateInferenceUsesSupportedInvariantFormats()
        {
            // Arrange
            var originalCulture = CultureInfo.CurrentCulture;
            var generator = new SampleJsonSchemaGenerator();
            var cultures = new[] { "en-US", "de-DE" };

            try
            {
                foreach (var cultureName in cultures)
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

                    // Act
                    var schema = generator.Generate("{ local: '10/12/2024', year: '2024', date: '2024-10-12', midnight: '2024-10-12T00:00:00', offset: '2024-10-12T00:00:00+02:00' }");

                    // Assert
                    Assert.Null(schema.Properties["local"].Format);
                    Assert.Null(schema.Properties["year"].Format);
                    Assert.Equal(JsonFormatStrings.Date, schema.Properties["date"].Format);
                    Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["midnight"].Format);
                    Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["offset"].Format);
                    Assert.Empty(schema.Validate("{ \"local\": \"10/12/2024\", \"year\": \"2024\", \"date\": \"2024-10-12\", \"midnight\": \"2024-10-12T00:00:00\", \"offset\": \"2024-10-12T00:00:00+02:00\" }"));
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        private sealed class TrackingMemoryStream(byte[] buffer) : MemoryStream(buffer)
        {
            public bool IsDisposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
