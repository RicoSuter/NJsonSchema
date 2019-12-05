using System;
using System.Collections;
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
        [InlineData("/References/YamlReferencesTest/yaml_spec_with_yaml_schema_refs.yaml")]
        public async Task When_yaml_spec_has_external_schema_refs_they_are_resolved(string relativePath)
        {
            var path = GetTestDirectory() + relativePath;

            //// Act
            Task<OpenApiDocument> docTask = OpenApiYamlDocument.FromFileAsync(path);
            OpenApiDocument doc = docTask.Result;
            IDictionary<string, OpenApiPathItem> docPaths = doc.Paths;
            OpenApiPathItem docPath = docPaths["/custom-queries"];
            OpenApiOperation getOp = docPath["get"];
            IDictionary<string, OpenApiResponse> responses = getOp.Responses;

            OpenApiResponse OK = responses["200"];
            OpenApiHeaders OKheaders = OK.Headers;

            OpenApiResponse Bad = responses["401"];

            Assert.NotNull(doc);
            Assert.IsType<JsonSchema>(OKheaders[""]);
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
