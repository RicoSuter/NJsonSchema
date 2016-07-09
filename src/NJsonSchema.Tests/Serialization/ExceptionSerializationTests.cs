using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Annotations;

namespace NJsonSchema.Tests.Serialization
{
    [TestClass]
    public class ExceptionSerializationTests
    {
        public class CompanyNotFoundException : Exception
        {
            public CompanyNotFoundException(string message) : base(message)
            {
            }

            public CompanyNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }

            [JsonProperty("CompanyKey")]
            public Guid CompanyKey { get; set; }
        }

        [TestMethod]
        public void When_custom_exception_is_serialized_then_everything_works()
        {
            //// Arrange
            try
            {
                throw new CompanyNotFoundException("Foo", new CompanyNotFoundException("Bar", new Exception("Hello World")));
            }
            catch (CompanyNotFoundException exception)
            {
                //// Act
                var json = JsonConvert.SerializeObject(exception, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters =
                    {
                        new JsonExceptionConverter()
                    },
                });

                //// Assert
                Assert.IsFalse(string.IsNullOrEmpty(json));
            }
        }
    }
}