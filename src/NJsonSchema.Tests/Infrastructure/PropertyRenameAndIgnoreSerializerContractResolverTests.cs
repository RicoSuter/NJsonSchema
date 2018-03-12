using Newtonsoft.Json;
using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Infrastructure
{
    public class PropertyRenameAndIgnoreSerializerContractResolverTests
    {
        public class MyClass
        {
            [JsonProperty("foo")]
            public string Foo { get; set; }
        }

        [Fact]
        public void When_property_is_renamed_then_json_is_correct()
        {
            //// Arrange
            var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            resolver.RenameProperty(typeof(MyClass), "foo", "bar");

            var obj = new MyClass();
            obj.Foo = "abc";

            //// Act
            var json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ContractResolver = resolver });
            obj = JsonConvert.DeserializeObject<MyClass>(json, new JsonSerializerSettings { ContractResolver = resolver });

            //// Assert
            Assert.Contains("bar", json);
            Assert.Contains("abc", obj.Foo);
        }
    }
}
