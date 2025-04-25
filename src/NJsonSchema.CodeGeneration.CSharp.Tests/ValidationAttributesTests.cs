using NJsonSchema.CodeGeneration.Tests;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class ValidationAttributesTests
    {
        [Fact]
        public async Task When_string_property_has_maxlength_then_stringlength_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/string50'
                    }
                },
                'definitions': {
                    'string50': {
                        'type': 'string',
                        'maxLength': 50
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].MaxLength);
            Assert.Equal(50, schema.Properties["value"].ActualSchema.MaxLength);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_string_property_has_minlength_then_stringlength_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/string40'
                    }
                },
                'definitions': {
                    'string40': {
                        'type': 'string',
                        'minLength': 40
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].MinLength);
            Assert.Equal(40, schema.Properties["value"].ActualSchema.MinLength);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_int_property_has_maximum_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int20'
                    }
                },
                'definitions': {
                    'int20': {
                        'type': 'integer',
                        'format': 'int32',
                        'maximum': 20
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Maximum);
            Assert.Equal(20, schema.Properties["value"].ActualSchema.Maximum);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_int32_property_has_minimum_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int10'
                    }
                },
                'definitions': {
                    'int10': {
                        'type': 'integer',
                        'format': 'int32',
                        'minimum': 10
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(10, schema.Properties["value"].ActualSchema.Minimum);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_int64_property_has_minimum_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int10'
                    }
                },
                'definitions': {
                    'int10': {
                        'type': 'integer',
                        'format': 'int64',
                        'minimum': 10
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(10, schema.Properties["value"].ActualSchema.Minimum);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_integer_property_has_minimum_and_maximum_that_are_int64_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int10000000000'
                    }
                },
                'definitions': {
                    'int10000000000': {
                        'type': 'integer',
                        'minimum': -10000000000,
                        'maximum': 10000000000,
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(-10000000000m, schema.Properties["value"].ActualSchema.Minimum);
            Assert.Equal(10000000000m, schema.Properties["value"].ActualSchema.Maximum);

            // expect the integer to be converted to an int64
            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_integer_property_has_minimum_and_maximum_with_exclusive_true_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int100000000'
                    }
                },
                'definitions': {
                    'int100000000': {
                        'type': 'integer',
                        'minimum': -100000000,
                        'exclusiveMinimum': true,
                        'maximum': 100000000,
                        'exclusiveMaximum': true,
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(-100000000m, schema.Properties["value"].ActualSchema.Minimum);
            Assert.Equal(100000000m, schema.Properties["value"].ActualSchema.Maximum);
            Assert.True(schema.Properties["value"].ActualSchema.IsExclusiveMaximum);
            Assert.True(schema.Properties["value"].ActualSchema.IsExclusiveMinimum);

            // expect the integer to be converted to an int64
            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_number_property_has_minimum_and_maximum_with_exclusive_true_and_multipleof_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/number'
                    }
                },
                'definitions': {
                    'number': {
                        'type': 'number',
                        'multipleOf': '0.0001',
                        'minimum': -100000000.5,
                        'exclusiveMinimum': true,
                        'maximum': 100000000.5,
                        'exclusiveMaximum': true,
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(-100000000.5m, schema.Properties["value"].ActualSchema.Minimum);
            Assert.Equal(100000000.5m, schema.Properties["value"].ActualSchema.Maximum);
            Assert.True(schema.Properties["value"].ActualSchema.IsExclusiveMaximum);
            Assert.True(schema.Properties["value"].ActualSchema.IsExclusiveMinimum);

            // expect the integer to be converted to an int64
            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_number_property_has_minimum_and_maximum_that_are_double_then_range_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/int10000000000'
                    }
                },
                'definitions': {
                    'int10000000000': {
                        'type': 'number',
                        'minimum': -10000000000,
                        'maximum': 10000000000,
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(-10000000000m, schema.Properties["value"].ActualSchema.Minimum);
            Assert.Equal(10000000000m, schema.Properties["value"].ActualSchema.Maximum);

            // expect the integer to be converted to an int64
            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact(Skip = "Existing bug")]
        public async Task When_number_property_has_minimum_and_maximum_that_are_decimal_then_range_attribute_is_rendered()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/theNumber'
                    }
                },
                'definitions': {
                    'theNumber': {
                        'type': 'number',
                        'format': 'decimal',
                        'minimum': -100.50,
                        'maximum': 100.50,
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Minimum);
            Assert.Equal(-100.50m, schema.Properties["value"].ActualSchema.Minimum);
            Assert.Equal(100.50m, schema.Properties["value"].ActualSchema.Maximum);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_string_property_has_pattern_then_regularexpression_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string regularExpression = "[a-zA-Z0-9]{5,56}";
            const string json = @"{
                        'type': 'object',
                        'required': [ 'value' ],
                        'properties': {
                            'value': {
                                '$ref': '#/definitions/stringPatterned'
                            }
                        },
                'definitions': {
                    'stringPatterned': {
                        'type': 'string',
                        'pattern': '" + regularExpression + @"'
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Null(schema.Properties["value"].Pattern);
            Assert.Equal(regularExpression, schema.Properties["value"].ActualSchema.Pattern);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_array_property_has_maxitems_then_maxlength_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/array10'
                    }
                },
                'definitions': {
                    'array10': {
                        'type': 'array',
                        'items': {
                           'type': 'string'
                        },
                        'maxItems': 10
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Equal(0, schema.Properties["value"].MaxItems);
            Assert.Equal(10, schema.Properties["value"].ActualSchema.MaxItems);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_array_property_has_minitems_then_minlength_attribute_is_rendered_in_Swagger_mode()
        {
            // Arrange
            const string json = @"{
                'type': 'object',
                'required': [ 'value' ],
                'properties': {
                    'value': {
                        '$ref': '#/definitions/array10'
                    }
                },
                'definitions': {
                    'array10': {
                        'type': 'array',
                        'items': {
                           'type': 'string'
                        },
                        'minItems': 10
                    }
                }
            }";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                SchemaType = SchemaType.Swagger2,
                InlineNamedArrays = true
            });
            var code = generator.GenerateFile("Message");

            // Assert
            Assert.Equal(0, schema.Properties["value"].MinItems);
            Assert.Equal(10, schema.Properties["value"].ActualSchema.MinItems);

            await VerifyHelper.Verify(code);

            CodeCompiler.AssertCompile(code);
        }
    }
}