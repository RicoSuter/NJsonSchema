using NJsonSchema.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class ReferencesTest
    {
        [Fact]
        public async Task When_ref_is_definitions_no_types_are_duplicated()
        {
            //// Arrange
            var path = GetTestDirectory() + "/References/E.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public enum C", code);
            Assert.DoesNotContain("public enum C2", code);
        }

        [Fact]
        public async Task When_ref_is_file_no_types_are_duplicated()
        {
            //// Arrange
            var path = GetTestDirectory() + "/References/A.json";

            //// Act
            var schema = await JsonSchema4.FromFileAsync(path);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public enum C", code);
            Assert.DoesNotContain("public enum C2", code);
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }
    }
}
