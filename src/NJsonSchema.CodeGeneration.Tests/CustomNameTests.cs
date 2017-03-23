using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Annotations;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class CustomNameTests
    {
        [JsonSchema("my-custom-name")]
        public class CustomNamedClass
        {
            [DefaultValue("foo")]
            public string Test { get; set; }
        }

        [TestMethod]
        public async Task Use_custom_name_in_schema()
        {
            //// Arrange
            
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<CustomNamedClass>();

            //// Assert
            Assert.AreEqual("my-custom-name", schema.Title);
        }

        [TestMethod]
        public async Task Use_custom_name_in_generated_csharp_code()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<CustomNamedClass>();

            //// Act
            var generator = new CSharpGenerator(schema);
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("[NJsonSchema.Annotations.JsonSchema(\"my-custom-name\")]"));
        }
    }
}