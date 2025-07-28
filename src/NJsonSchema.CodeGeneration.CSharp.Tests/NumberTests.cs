using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;

namespace NJsonSchema.CodeGeneration.Tests.CSharp;

public class NumberTests
{
    [Fact]
    public async Task When_number_has_no_format_then_default_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema);

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }

    [Fact]
    public async Task When_number_has_decimal_format_then_decimal_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"": ""decimal""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema);

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }

    [Fact]
    public async Task When_number_has_double_format_then_double_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"": ""double""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema);

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }

    [Fact]
    public async Task When_number_has_float_format_then_float_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"": ""float""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema);

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }

    [Fact]
    public async Task When_number_type_setting_is_defined_then_setting_type_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberType = "customNumberType"
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
    }

    [Fact]
    public async Task When_number_type_setting_is_whitespace_then_double_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number""
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberType = " \t\n"
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }

    [Fact]
    public async Task When_number_type_setting_is_null_then_double_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""nullable"": true
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberType = null!
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
        CSharpCompiler.AssertCompile(code);
    }
   
    [Fact]
    public async Task When_number_float_type_setting_is_defined_then_setting_type_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"":""float"",
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberFloatType = "customFloatType"
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
    }

    [Fact]
    public async Task When_number_double_type_setting_is_defined_then_setting_type_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"":""double"",
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberDoubleType = "customDoubleType"
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
    }

    [Fact]
    public async Task When_number_decimal_type_setting_is_defined_then_setting_type_is_generated()
    {
        // Arrange
        var json =
            @"{
                ""type"": ""object"", 
                ""properties"": {
                    ""amount"" : {
                        ""type"":""number"",
                        ""format"":""decimal"",
                    }
                }
            }";
        var schema = await JsonSchema.FromJsonAsync(json);
        var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings
        {
            NumberDecimalType = "customDecimalType"
        });

        // Act
        var code = generator.GenerateFile("MyClass");

        // Assert
        await VerifyHelper.Verify(code);
    }
}