using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.Tests.Models;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class TypeScriptGeneratorTests
    {
        [TestMethod]
        public void When_property_name_does_not_match_property_name_then_casing_is_correct_in_output()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"lastName?: string;"));
            Assert.IsTrue(output.Contains(@"Dictionary?: { [key: string] : number; };"));
        }

        [TestMethod]
        public void When_property_is_required_name_then_TypeScript_property_is_not_optional()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"FirstName: string;"));
        }

        [TestMethod]
        public void When_allOf_contains_one_schema_then_csharp_inheritance_is_generated()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"interface Teacher extends Person"));
        }

        [TestMethod]
        public void When_enum_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.AllOf.First().Properties["Gender"].Description = "EnumDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/** EnumDesc. *"));
        }
        
        [TestMethod]
        public void When_class_has_description_then_typescript_has_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.Description = "ClassDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/** ClassDesc. *"));
        }

        [TestMethod]
        public void When_property_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.Properties["Class"].Description = "PropertyDesc.";
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/** PropertyDesc. *"));
        }

        [TestMethod]
        public void When_property_is_readonly_then_ts_property_is_also_readonly()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"readonly Birthday"));
        }

        [TestMethod]
        public void When_name_contains_dash_then_it_is_converted_to_upper_case()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.TypeName = "MyClass";
            schema.Properties["foo-bar"] = new JsonProperty
            {
                Type = JsonObjectType.String
            };

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"""foo-bar""?: string;"));
        }

        [TestMethod]
        public void When_type_name_is_missing_then_anonymous_name_is_generated()
        {
            //// Arrange
            var schema = new JsonSchema4();

            var generator = new TypeScriptGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsFalse(output.Contains(@"interface  {"));
        }

        private static TypeScriptGenerator CreateGenerator()
        {
            var schema = JsonSchema4.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var generator = new TypeScriptGenerator(schema);
            return generator;
        }
    }
}
