using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class JsonPathUtilitiesGetObjectFromJsonPathTests
    {
        [TestMethod]
        public async Task When_object_is_in_property_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    Property1 = new { },
                    Property2 = objectToSearch
                }
            };

            //// Act
            var resolver = new JsonReferenceResolver(null);
            var foundObject = await resolver.ResolveReferenceAsync(obj, "#/Property/Property2");

            //// Assert
            Assert.AreEqual(foundObject, objectToSearch);
        }

        [TestMethod]
        public async Task When_object_is_in_list_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    List = new List<object>
                    {
                        new { },
                        new { },
                        objectToSearch
                    }
                }
            };

            //// Act
            var resolver = new JsonReferenceResolver(null);
            var foundObject = await resolver.ResolveReferenceAsync(obj, "#/Property/List/2");

            //// Assert
            Assert.AreEqual(foundObject, objectToSearch);
        }

        [TestMethod]
        public async Task When_object_is_in_dictionary_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();
            var obj = new
            {
                Property = new
                {
                    List = new Dictionary<string, object>
                    {
                        { "Test1", new { } },
                        { "Test2", new { } },
                        { "Test3", objectToSearch },
                    }
                }
            };

            //// Act
            var resolver = new JsonReferenceResolver(null);
            var foundObject = await resolver.ResolveReferenceAsync(obj, "#/Property/List/Test3");

            //// Assert
            Assert.AreEqual(foundObject, objectToSearch);
        }

        [TestMethod]
        public async Task When_object_is_root_then_path_should_be_built_correctly()
        {
            //// Arrange
            var objectToSearch = new JsonSchema4();

            //// Act
            var resolver = new JsonReferenceResolver(null);
            var foundObject = await resolver.ResolveReferenceAsync(objectToSearch, "#");

            //// Assert
            Assert.AreEqual(foundObject, objectToSearch);
        }

        [TestMethod]
        public async Task When_object_is_in_external_file_then_path_should_be_built_correctly()
        {
            //// Arrange
            var referencingSchema = new JsonSchema4
            {
                DocumentPath = "other_schema.json",
                Reference = new JsonSchema4
                {
                    DocumentPath = "some_schema.json"
                }
            };

            //// Act
            var result = referencingSchema.ToJsonWithExternalReferences();

            //// Assert
            Assert.IsTrue(result.Contains("some_schema.json"));
        }
    }
}