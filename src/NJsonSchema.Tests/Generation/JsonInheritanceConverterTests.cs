using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class JsonInheritanceConverterTests
    {
        private static readonly JsonSerializer DefaultSerializer = JsonSerializer.CreateDefault();

        [KnownType(typeof(ClassA))]
        public class BaseClass
        {
            public string PropertyA { get; set; } = "defaultA";
        }

        public class ClassA : BaseClass
        {
            public string PropertyB { get; set; } = "defaultB";
        }

        [Fact]
        public void When_serializing_discriminator_property_is_set()
        {
            // Arrange
            var objA = new ClassA();
            var stringWriter = new StringWriter();
            var textWriter = new JsonTextWriter(stringWriter);

            // Act
            new JsonInheritanceConverter("discriminator").WriteJson(textWriter, objA, DefaultSerializer);

            // Assert
            var json = stringWriter.ToString();
            Assert.Contains("\"discriminator\":\"ClassA\"", json);
            Assert.Contains("\"PropertyA\":\"defaultA\"", json);
            Assert.Contains("\"PropertyB\":\"defaultB\"", json);
        }

        [Fact]
        public void When_serializing_discriminator_property_is_overwritten_if_already_present()
        {
            // Arrange
            var objA = new ClassA();
            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);

            // Act
            new JsonInheritanceConverter("PropertyA").WriteJson(jsonWriter, objA, DefaultSerializer);

            // Assert
            var json = stringWriter.ToString();
            Assert.Contains("\"PropertyA\":\"ClassA\"", json);
            Assert.Contains("\"PropertyB\":\"defaultB\"", json);
        }

        [Fact]
        public void When_deserializing_type_is_resolved_using_discriminator_value()
        {
            // Arrange
            var json = @"
                {
                    ""PropertyA"":""v1"",
                    ""PropertyB"":""v2"",
                    ""discriminator"":""ClassA""
                }";
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var obj = new JsonInheritanceConverter("discriminator").ReadJson(jsonReader, typeof(BaseClass), null, DefaultSerializer);

            // Assert
            Assert.IsType<ClassA>(obj);

            var objA = (ClassA)obj;
            Assert.Equal("v1", objA.PropertyA);
            Assert.Equal("v2", objA.PropertyB);
        }

        [Fact]
        public void When_deserializing_existing_property_is_populated_with_discriminator_value()
        {
            // Arrange
            var json = @"
                {
                    ""PropertyA"":""ClassA"",
                    ""PropertyB"":""v2""
                }";
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var obj = new JsonInheritanceConverter("PropertyA").ReadJson(jsonReader, typeof(BaseClass), null, DefaultSerializer);

            // Assert
            Assert.IsType<ClassA>(obj);

            var objA = (ClassA)obj;
            Assert.Equal("ClassA", objA.PropertyA);
            Assert.Equal("v2", objA.PropertyB);
        }
    }
}
