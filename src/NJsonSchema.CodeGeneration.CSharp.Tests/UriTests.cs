using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class UriTests
    {
        public class ClassWithUri
        {
            public Uri MyUri { get; set; }
        }

        [Fact]
        public async Task When_property_is_uri_then_csharp_output_is_also_uri()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithUri>();
            var json = schema.ToJson();
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public System.Uri MyUri", code);
        }

        [Fact]
        public void When_uri_is_relative_then_it_is_serialized_and_deserialized_correctly()
        {
            //// Arrange
            var obj = new ClassWithUri { MyUri = new Uri("abc/def", UriKind.Relative) };

            //// Act
            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<ClassWithUri>(json);

            //// Assert
            Assert.Equal("{\"MyUri\":\"abc/def\"}", json);
            Assert.Equal(obj.MyUri, obj2.MyUri);
        }

        [Fact]
        public void When_uri_is_absolute_then_it_is_serialized_and_deserialized_correctly()
        {
            //// Arrange
            var obj = new ClassWithUri { MyUri = new Uri("https://abc/def", UriKind.Absolute) };

            //// Act
            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<ClassWithUri>(json);

            //// Assert
            Assert.Equal("{\"MyUri\":\"https://abc/def\"}", json);
            Assert.Equal(obj.MyUri, obj2.MyUri);
        }
    }
}