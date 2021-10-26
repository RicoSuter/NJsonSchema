using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NSwag;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Yaml.Tests.References
{
    public class LocalReferencesTests
    {
        [Theory]
        [InlineData("/References/YamlReferencesTest/json_schema_with_json_reference.json", "./collection.json")]
        [InlineData("/References/YamlReferencesTest/yaml_schema_with_json_reference.yaml", "./collection.json")]
        [InlineData("/References/YamlReferencesTest/yaml_schema_with_yaml_reference.yaml", "./collection.yaml")]
        [InlineData("/References/YamlReferencesTest/json_schema_with_yaml_reference.json", "./collection.yaml")]
        public async Task When_yaml_schema_has_references_it_works(string relativePath, string documentPath)
        {
            //// Arrange
            var path = GetTestDirectory() + relativePath;

            //// Act
            var schema = await JsonSchemaYaml.FromFileAsync(path);
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["foo"].ActualTypeSchema.Type);
            Assert.Equal(1, schema.Definitions.Count);
            Assert.Equal(documentPath, schema.Definitions["collection"].DocumentPath);
        }

        //[Theory]
        //[InlineData("/References/YamlReferencesTest/yaml_spec_with_yaml_schema_refs.yaml", "/custom-queries", "Content-Language")]
        public async Task When_yaml_OpenAPI_spec_has_external_schema_refs_they_are_resolved(string relativePath, string docPath, string header)
        {
            var path = GetTestDirectory() + relativePath;

            //// Act
            OpenApiDocument doc = await OpenApiYamlDocument.FromFileAsync(path);
            IDictionary<string, OpenApiPathItem> docPaths = doc.Paths;
            OpenApiPathItem pathItem = docPaths[docPath];
            OpenApiOperation operation = pathItem["get"];
            IDictionary<string, OpenApiResponse> responses = operation.Responses;

            OpenApiResponse OK = responses["200"].ActualResponse;
            OpenApiHeaders OKheaders = OK.Headers;

            OpenApiResponse Unauthorized = responses["401"].ActualResponse;

            ////Assert
            
            // Header schema loaded correctly from headers.yaml
            Assert.True(OKheaders.ContainsKey(header));
            Assert.NotNull(OKheaders[header]);

            //Response data loaded correctly from responses.yaml
            string problemType = "application/problem+json";
            Assert.True(Unauthorized.Content.ContainsKey(problemType));
            Assert.NotNull(Unauthorized.Content[problemType]);
            Assert.NotNull(Unauthorized.Schema);
        }

        //[Theory]
        //[InlineData("/References/YamlReferencesTest/subdir_spec/yaml_spec_with_yaml_schema_with_relative_subdir_refs.yaml")]
        public async Task When_yaml_OpenAPI_spec_has__relative_external_schema_refs_in_subdirs__they_are_resolved(string relativePath)
        {
            var path = GetTestDirectory() + relativePath;

            OpenApiDocument doc = await OpenApiYamlDocument.FromFileAsync(path);
            IDictionary<string, OpenApiPathItem> docPaths = doc.Paths;

            OpenApiPathItem pathItem = docPaths["/life-cycles"];
            OpenApiOperation operation = pathItem["get"];
            IDictionary<string, OpenApiResponse> responses = operation.Responses;
            OpenApiResponse OK = responses["200"].ActualResponse;
            var schema = OK.Content["application/json"];
            JsonSchemaProperty items = schema.Schema.ActualSchema.ActualProperties["items"];
            var innerProperties = items.Item.ActualSchema.ActualProperties;
            string[] expectedProperties = new string[] { "id", "systemName", "name", "smallImageID", "helpText" };

            foreach (string property in expectedProperties)
            {
                Assert.True(innerProperties.ContainsKey(property));
            }

            pathItem = docPaths["/ad-hoc-tasks/{adhocTaskId}/execute"];
            operation = pathItem["post"];
            responses = operation.Responses;
            OK = responses["200"].ActualResponse;
            schema = OK.Content["application/json"];

            Assert.Equal("status", schema.Schema.ActualDiscriminator);
            Assert.Equal("Completed", schema.Schema.ActualDiscriminatorObject.Mapping.Keys.First());
            Assert.Equal(2, schema.Schema.ActualSchema.ActualProperties["status"].ActualSchema.Enumeration.Count);
        }
        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
