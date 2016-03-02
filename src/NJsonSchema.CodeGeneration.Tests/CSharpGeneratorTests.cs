using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class CSharpGeneratorTests
    {
        [TestMethod]
        public void When_namespace_is_set_then_it_should_appear_in_output()
        {
            //// Arrange
            var generator = CreateGenerator();
            
            //// Act
            var output = generator.GenerateFile();
            
            //// Assert
            Assert.IsTrue(output.Contains("namespace MyNamespace"));
            Assert.IsTrue(output.Contains("Dictionary<string, long>"));
        }

        [TestMethod]
        public void When_POCO_is_set_then_auto_properties_is_available()
        {
            //// Arrange
            var generator = CreateGenerator();
            generator.Settings.ClassStyle = CSharpClassStyle.Poco;

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains("{ get; set; }"));
        }

        [TestMethod]
        public void When_property_name_does_not_match_property_name_then_attribute_is_correct()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"[JsonProperty(""lastName"""));
            Assert.IsTrue(output.Contains(@"public string LastName"));
        }

        [TestMethod]
        public void When_property_is_timespan_than_csharp_timespan_is_used()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Person>();
            var data = schema.ToJson();
            var generator = new CSharpGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"public TimeSpan TimeSpan"));
        }
        
        [TestMethod]
        public void When_allOf_contains_one_schema_then_csharp_inheritance_is_generated()
        {
            //// Arrange
            var generator = CreateGenerator();

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"class Teacher : Person, "));
        }

        [TestMethod]
        public void When_enum_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.AllOf.First().Properties["Gender"].Description = "EnumDesc.";
            var generator = new CSharpGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/// <summary>EnumDesc.</summary>"));
        }

        [TestMethod]
        public void When_class_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.Description = "ClassDesc.";
            var generator = new CSharpGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/// <summary>ClassDesc.</summary>"));
        }

        [TestMethod]
        public void When_property_has_description_then_csharp_has_xml_comment()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            schema.Properties["Class"].Description = "PropertyDesc.";
            var generator = new CSharpGenerator(schema);

            //// Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains(@"/// <summary>PropertyDesc.</summary>"));
        }

        private static CSharpGenerator CreateGenerator()
        {
            var schema = JsonSchema4.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var settings = new CSharpGeneratorSettings();
            settings.Namespace = "MyNamespace";
            var generator = new CSharpGenerator(schema, settings);
            return generator;
        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }
        
        public DateTime Birthday { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public Gender Gender { get; set; }
        
        public Address Address { get; set; }

        public List<string> Array { get; set; } 

        public Dictionary<string, int> Dictionary { get; set; } 
    }

    public class Teacher : Person
    {
        public string Class { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }

        public string City { get; set; }
    }

    public enum Gender
    {
        Male, 
        Female
    }
}
