using NJsonSchema.CodeGeneration.Tests;

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

            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
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

            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
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

            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
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

            // Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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

            // Act
            var settings = new CSharpGeneratorSettings
            {
                GenerateDefaultValues = true
            };

            var generator = new CSharpGenerator(document, settings);
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
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
            await VerifyHelper.Verify(code);
            CSharpCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_property_has_const_string_value_then_property_is_readonly_with_default_value()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": {
                        ""const"": ""person""
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_const_integer_value_then_property_is_readonly_with_default_value()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""myNumber"": {
                        ""const"": 42
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(output);
            CSharpCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_const_boolean_value_then_property_is_readonly_with_default_value()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""isActive"": {
                        ""const"": true
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            // Assert
            Assert.Contains("public bool IsActive { get; } = true;", output);
            CSharpCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_const_with_type_then_type_is_used()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""cmdType"": {
                        ""type"": ""string"",
                        ""const"": ""person""
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            // Assert
            Assert.Contains("public string CmdType { get; } = \"person\";", output);
            CSharpCompiler.AssertCompile(output);
        }

        [Fact]
        public async Task When_property_has_const_double_value_then_property_is_readonly_with_default_value()
        {
            // Arrange
            var data = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""rate"": {
                        ""const"": 3.14
                    }
                }
            }";

            var schema = await JsonSchema.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns"
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            // Assert
            Assert.Contains("public double Rate { get; } = 3.14", output);
            CSharpCompiler.AssertCompile(output);
        }
    }
}
