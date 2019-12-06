using System;
using System.Collections.Generic;
using System.IO;
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

        [Theory]
        [InlineData("/References/YamlReferencesTest/yaml_spec_with_yaml_schema_refs.yaml", "/custom-queries", "Content-Language")]
        public async Task When_yaml_spec_has_external_schema_refs_they_are_resolved(string relativePath, string docPath, string header)
        {
            var path = GetTestDirectory() + relativePath;

            //// Act
            Task<OpenApiDocument> docTask = OpenApiYamlDocument.FromFileAsync(path);
            OpenApiDocument doc = docTask.Result;
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

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
