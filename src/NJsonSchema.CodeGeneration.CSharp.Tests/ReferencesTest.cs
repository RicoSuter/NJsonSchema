using Newtonsoft.Json.Serialization;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Infrastructure;
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
            var schema = await JsonSchema.FromFileAsync(path);
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
            var schema = await JsonSchema.FromFileAsync(path);
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public enum C", code);
            Assert.DoesNotContain("public enum C2", code);
        }

        [Fact]
        public async Task When_ref_is_file_and_it_contains_nullable_property_then_generated_property_is_also_nullable()
        {
            //// Arrange
            var path = GetTestDirectory() + "/References/F.json";

            //// Act
            var schema = await JsonSchema.FromFileAsync(path);
            var generatorSettings = new CSharpGeneratorSettings
            {
                GenerateNullableReferenceTypes = true
            };
            var generator = new CSharpGenerator(schema, generatorSettings);

            //// Act
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("public string? Name", code);
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }

        [Fact]
        public async Task When_inheritance_with_object_without_props_is_generated_then_all_classes_exist_and_additional_properties_property_is_not_generated()
        {
            // Arrange
            var json = @"{
  ""type"": ""object"",
  ""properties"": {
    ""Exception"": {
      ""$ref"": ""#/definitions/BusinessException""
    }
  },
  ""additionalProperties"": false,
  ""definitions"": {
    ""BusinessException"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""customerId"": {
          ""type"": ""string"",
          ""nullable"": true
        },
        ""customerAlias"": {
          ""type"": ""string"",
          ""nullable"": true
        },
        ""userId"": {
          ""type"": ""string"",
          ""nullable"": true
        }
      }
    },
    ""ValidationException"": {
      ""allOf"": [
        {
          ""$ref"": ""#/definitions/BusinessException""
        },
        {
          ""type"": ""object"",
          ""additionalProperties"": false
        }
      ]
    }
  }
}";

            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, SchemaType.OpenApi3, null, factory, new DefaultContractResolver());

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
            var code = generator.GenerateFile("MyClass");

            //// Act
            Assert.Contains("class BusinessException", code);
            Assert.Contains("class ValidationException", code);
            Assert.DoesNotContain("AdditionalProperties", code);
        }
        
        [Fact]
        public async Task When_date_reference_is_generated_from_swagger2_schema_then_generated_member_is_decorated_with_date_format_attribute()
        {
            // Arrange
            var json = @"{
  ""type"": ""object"",
  ""properties"": {
    ""MyType"": {
      ""$ref"": ""#/definitions/MyType""
    }
  },
  ""additionalProperties"": false,
	""definitions"": {
		""MyType"": {
			""type"": ""object"",
			""required"": [
				""EntryDate"",
			],
			""properties"": {
			    ""EntryDate"": {
				    ""$ref"": ""#/definitions/EntryDate""
				}
			}
		},
		""EntryDate"": {
			""example"": ""2020-08-28"",
			""type"": ""string"",
			""format"": ""date""
		}
	}
}";

            var factory = JsonReferenceResolver.CreateJsonReferenceResolverFactory(new DefaultTypeNameGenerator());
            var schema = await JsonSchemaSerialization.FromJsonAsync(json, SchemaType.Swagger2, null, factory, new DefaultContractResolver());

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { SchemaType = SchemaType.Swagger2 });
            var code = generator.GenerateFile("MyClass");

            //// Act
            Assert.Contains("[Newtonsoft.Json.JsonConverter(typeof(DateFormatConverter))]", code);
        }
    }
}
