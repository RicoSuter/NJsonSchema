using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class DefaultPropertyTests
    {
        [Fact]
        public async Task When_property_has_interger_default_it_is_reflected_in_the_poco()
        {
            var data = @"{'properties': {
                                'intergerWithDefault': {      
                                    'type': 'integer',
                                    'format': 'int32',
                                    'default': 5
                                 }
                             }}";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = true
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public int IntergerWithDefault { get; set; } = 5;", output);
        }

        [Fact]
        public async Task When_property_has_boolean_default_it_is_reflected_in_the_poco()
        {
            var data = @"{'properties': {
                                'boolWithDefault': {
                                    'type': 'boolean',
                                    'default': false
                                 }
                             }}";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = true
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public bool BoolWithDefault { get; set; } = false;", output);
        }

        [Fact]
        public async Task When_property_has_boolean_default_and_default_value_generation_is_disabled_then_default_value_is_not_generated()
        {
            var data = @"{'properties': {
                                'boolWithDefault': {
                                    'type': 'boolean',
                                    'default': false
                                 }
                             }}";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = false
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public bool BoolWithDefault { get; set; }", output);
            Assert.DoesNotContain("public bool BoolWithDefault { get; set; } = false;", output);
        }

        [Fact]
        public async Task When_generating_CSharp_code_then_default_value_generates_expected_expression()
        {
            // Arrange
            var document = await JsonSchema.FromJsonAsync(@"{
              ""type"": ""object"",
              ""properties"": {
                ""someOptionalProperty"": {
                  ""type"": ""number"",
                  ""default"": ""123""
                }
              }
            }");

            //// Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            Assert.DoesNotContain("SomeOptionalProperty { get; set; } = D;", code);
            Assert.Contains("double SomeOptionalProperty { get; set; } = 123D;", code);
        }

        [Fact]
        public async Task When_generating_CSharp_code_then_default_value_with_decimal_generates_expected_expression()
        {
            // Arrange
            var document = await JsonSchema.FromJsonAsync(@"{
              ""type"": ""object"",
              ""properties"": {
                ""someOptionalProperty"": {
                  ""type"": ""number"",
                  ""default"": ""123.456""
                }
              }
            }");

            //// Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            Assert.DoesNotContain("SomeOptionalProperty { get; set; } = D;", code);
            Assert.Contains("double SomeOptionalProperty { get; set; } = 123.456D;", code);
        }
        
        [Fact]
        public async Task When_generating_CSharp_code_then_default_value_of_dictionary_with_array_values_generates_expected_expression()
        {
            // Arrange
            var document = await JsonSchema.FromJsonAsync(@"{
              ""type"": ""object"",
              ""required"": [""requiredDictionary""],
              ""properties"": {
                ""requiredDictionary"": {
                  ""type"": ""object"",
                  ""additionalProperties"": {
                    ""type"": ""array"",
                    ""items"": {
                      ""type"": ""string""                
                    }
                  }
                }
              }
            }");

            // Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            Assert.DoesNotContain("System.Collections.Generic.IDictionary<string, System.Collections.Generic.ICollection<string>> RequiredDictionary { get; set; } = new System.Collections.Generic.Dictionary<string, System.Collections.ObjectModel.Collection<string>>();", code);
            Assert.Contains("System.Collections.Generic.IDictionary<string, System.Collections.Generic.ICollection<string>> RequiredDictionary { get; set; } = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.ICollection<string>>();", code);
        }

        [Fact]
        public async Task When_generating_CSharp_code_then_default_value_of_array_of_arrays_generates_expected_expression()
        {
            // Arrange
            var document = await JsonSchema.FromJsonAsync(@"{
              ""type"": ""object"",
              ""required"": [""requiredList""],
              ""properties"": {
                ""requiredList"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""type"": ""array"",
                    ""items"": {
                      ""type"": ""string""                
                    }
                  }
                }
              }
            }");

            // Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            Assert.DoesNotContain("System.Collections.Generic.ICollection<System.Collections.Generic.ICollection<string>> RequiredList { get; set; } = new System.Collections.ObjectModel.Collection<System.Collections.ObjectModel.Collection<string>>();", code);
            Assert.Contains("System.Collections.Generic.ICollection<System.Collections.Generic.ICollection<string>> RequiredList { get; set; } = new System.Collections.ObjectModel.Collection<System.Collections.Generic.ICollection<string>>();", code);
        }
    }
}
