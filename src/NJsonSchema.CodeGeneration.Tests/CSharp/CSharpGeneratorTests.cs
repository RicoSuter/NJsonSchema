using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.Tests.Models;
using System;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
    public class CSharpGeneratorTests
    {
        [TestMethod]
        public void multiple_refs_in_all_of_should_expand_to_single_def()
        {
            var schema = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'id': 'http://some.domain.com/foo.json',
                'type': 'object',
                'additionalProperties': false,
                'definitions': {
                    'tRef1': {
                        'type': 'object',
                        'properties': {
                            'val1': {
                                'type': 'string',
                            }
                        }
                    },
                    'tRef2': {
                        'type': 'object',
                        'properties': {
                            'val2': {
                                'type': 'string',
                            }
                        }
                    },
                    'tRef3': {
                        'type': 'object',
                        'properties': {
                            'val3': {
                                'type': 'string',
                            }
                        }
                    }
                },
                'tAgg': {
                    'allOf': [
                        {'$ref': '#/definitions/tRef1'},
                        {'$ref': '#/definitions/tRef2'},
                        {'$ref': '#/definitions/tRef3'}
                    ]
                }
            }";
            var s = NJsonSchema.JsonSchema4.FromJson(schema);
            var settings = new CSharpGeneratorSettings() { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns", };
            var gen = new CSharpGenerator(s, settings);
            var output = gen.GenerateFile();
            System.Console.WriteLine(output);
        } 

        class CustomPropertyNameGenerator : IPropertyNameGenerator
        {
            public string Generate(JsonProperty property)
            {
                return "MyCustom" + ConversionUtilities.ConvertToUpperCamelCase(property.Name);
            }
        }
        class CustomTypeNameGenerator : ITypeNameGenerator
        {
            public string Generate(JsonSchema4 schema)
            {
                return "MyCustomType" + ConversionUtilities.ConvertToUpperCamelCase(schema.TypeNameRaw);
            }

        }

        [TestMethod]
        public void When_property_name_is_created_by_custom_fun_then_attribute_is_correct()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<Teacher>();
            var schemaData = schema.ToJson();
            var settings = new CSharpGeneratorSettings();

            settings.TypeNameGenerator = new CustomTypeNameGenerator();
            settings.PropertyNameGenerator = new CustomPropertyNameGenerator();
            var generator = new CSharpGenerator(schema, settings);

            //// Act
            var output = generator.GenerateFile();
            Console.WriteLine(output);

            //// Assert
            Assert.IsTrue(output.Contains(@"[JsonProperty(""lastName"""));
            Assert.IsTrue(output.Contains(@"public string MyCustomLastName"));
            Assert.IsTrue(output.Contains(@"public partial class MyCustomTypeTeacher"));
            Assert.IsTrue(output.Contains(@"public partial class MyCustomTypePerson"));
        }

        [TestMethod]
        public void When_schema_contains_ref_to_definition_that_refs_another_definition_then_result_should_contain_correct_target_ref_type()
        {
            //// Arrange
            var schemaJson =
@"{
	'x-typeName': 'foo',
	'type': 'object',
	'definitions': {
		'pRef': {
			'type': 'object',
			'properties': {
				'pRef2': {
					'$ref': '#/definitions/pRef2'
				},
				
			}
		},
		'pRef2': {
			'type': 'string'
		}
	},
	'properties': {
		'pRefs': {
			'type': 'array',
			'items': {
				'$ref': '#/definitions/pRef'
			}
		}
	}
}";

            var schema = JsonSchema4.FromJson(schemaJson);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco
            };
            var gen = new CSharpGenerator(schema, settings);

            //// Act
            var output = gen.GenerateFile();

            //// Assert
            Assert.IsTrue(output.Contains("public ObservableCollection<pRef>"));
        }

        [TestMethod]
        public void When_property_has_boolean_default_it_is_reflected_in_the_poco()
        {
            var schema = @"{'properties': {
                                'boolWithDefault': {
                                    'type': 'boolean',
                                    'default': false
                                 }
                             }}";

            var s = NJsonSchema.JsonSchema4.FromJson(schema);
            var settings = new CSharpGeneratorSettings() { ClassStyle = CSharpClassStyle.Poco, Namespace = "ns", };
            var gen = new CSharpGenerator(s, settings);
            var output = gen.GenerateFile();

            Assert.IsTrue(output.Contains("public bool BoolWithDefault { get; set; } = false"));
        }

        [TestMethod]
        public void When_namespace_is_set_then_it_should_appear_in_output()
        {
            //// Arrange
            var generator = CreateGenerator();
            
            //// Act
            var output = generator.GenerateFile();
            
            //// Assert
            Assert.IsTrue(output.Contains("namespace MyNamespace"));
            Assert.IsTrue(output.Contains("Dictionary<string, int>"));
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

        [TestMethod]
        public void Can_generate_type_from_string_property_with_byte_format()
        {
            // Arrange
            var schema = JsonSchema4.FromType<File>();
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile();

            // Assert
            Assert.IsTrue(output.Contains("public byte[] Content"));
        }

        [TestMethod]
        public void Can_generate_type_from_string_property_with_base64_format()
        {
            // Arrange
            var schema = JsonSchema4.FromType<File>();
            schema.Properties["Content"].Format = "base64";
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile();

            // Assert
            Assert.IsTrue(output.Contains("public byte[] Content"));
        }

        [TestMethod]
        public void When_name_contains_dash_then_it_is_converted_to_upper_case()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.TypeNameRaw = "MyClass";
            schema.Properties["foo-bar"] = new JsonProperty
            {
                Type = JsonObjectType.String
            };

            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile();

            // Assert
            Assert.IsTrue(output.Contains(@"[JsonProperty(""foo-bar"", "));
            Assert.IsTrue(output.Contains(@"public string FooBar"));
        }

        [TestMethod]
        public void When_type_name_is_missing_then_anonymous_name_is_generated()
        {
            //// Arrange
            var schema = new JsonSchema4();
            var generator = new CSharpGenerator(schema);

            // Act
            var output = generator.GenerateFile();

            //// Assert
            Assert.IsFalse(output.Contains(@"class  :"));
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
}
