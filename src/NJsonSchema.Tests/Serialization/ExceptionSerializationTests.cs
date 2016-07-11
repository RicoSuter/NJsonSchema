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
            internal CompanyNotFoundException()
            {
            }

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
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                //ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters =
                {
                    new JsonExceptionConverter()
                },
            };
            try
            {
                throw new CompanyNotFoundException("Foo", new CompanyNotFoundException("Bar", new Exception("Hello World")))
                {
                    CompanyKey = new Guid("E343DE26-1F13-4FE4-9368-5518E79DDBB9")
                };
            }
            catch (CompanyNotFoundException exception)
            {
                //// Act
                var json = JsonConvert.SerializeObject(exception, settings);
                var newException = JsonConvert.DeserializeObject<CompanyNotFoundException>(json, settings);

                //// Assert
                Assert.AreEqual(exception.CompanyKey, newException.CompanyKey);
                Assert.AreEqual(exception.Message, newException.Message);
            }
        }
    }
}