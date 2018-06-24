using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Yaml.Tests.References
{
    public class LocalReferencesTests
    {
        [Theory]
        [InlineData("/References/YamlReferencesTest/json_schema_with_json_reference.json")]
        [InlineData("/References/YamlReferencesTest/yaml_schema_with_json_reference.yaml")]
        [InlineData("/References/YamlReferencesTest/yaml_schema_with_yaml_reference.yaml")]
        [InlineData("/References/YamlReferencesTest/json_schema_with_yaml_reference.json")]
        public async Task When_yaml_schema_has_references_it_works(string relativePath)
        {
            //// Arrange
            var path = GetTestDirectory() + relativePath;

            //// Act
            var schema = await JsonSchemaYaml.FromFileAsync(path);
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["foo"].ActualTypeSchema.Type);
            Assert.Equal(1, schema.Definitions.Count);
            Assert.Equal("./collection.json", schema.Definitions["collection"].DocumentPath);
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
