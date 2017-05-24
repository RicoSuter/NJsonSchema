using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class ExceptionTypeTests
    {
        public class MyException : Exception
        {
            [JsonProperty("foo")]
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_exception_schema_is_generated_then_special_properties_are_generated_and_JsonProperty_attribute_used()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyException>();
            var json = schema.ToJson();

            //// Act
            var exceptionSchema = schema.InheritedSchema.ActualSchema;

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("foo"));
            Assert.IsTrue(exceptionSchema.Properties.ContainsKey("InnerException"));
        }
    }
}
