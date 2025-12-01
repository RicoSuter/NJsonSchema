using Fluid;
using NJsonSchema.CodeGeneration.CSharp;

namespace NJsonSchema.CodeGeneration.Tests;

public class TemplateFactoryTests
{
    [Fact]
    public void Can_customize_liquid_parser()
    {
        var generatorSettings = new CSharpGeneratorSettings
        {
            TemplateDirectory = "Templates",
        };

        var factory = new DefaultTemplateFactory(generatorSettings, [typeof(CSharpGeneratorSettings).Assembly])
        {
            FluidParser = new LiquidParser(new FluidParserOptions { AllowLiquidTag = true }),
        };
        generatorSettings.TemplateFactory = factory;

        var template = factory.CreateTemplate("csharp", "inline-liquid", new { });

        var templateResult = template.Render();

        Assert.Equal("WELCOME TO THE LIQUID TAG", templateResult);
    }

    [Theory]
    [InlineData("Templates", "sample-1", "sample-1-in-templates")]
    [InlineData("Templates", "sample-2", "sample-2-in-templates")]
    [InlineData("Templates2", "sample-2", "sample-2-in-templates-2")]
    [InlineData("Templates2", "sample-3", "sample-3-in-templates-2")]
    [InlineData("Templates2;Templates", "sample-1", "sample-1-in-templates")]
    [InlineData("Templates2;Templates", "sample-2", "sample-2-in-templates-2")]
    [InlineData("Templates2;Templates", "sample-3", "sample-3-in-templates-2")]
    [InlineData("Templates;Templates2", "sample-1", "sample-1-in-templates")]
    [InlineData("Templates;Templates2", "sample-2", "sample-2-in-templates")]
    [InlineData("Templates;Templates2", "sample-3", "sample-3-in-templates-2")]
    public void Can_override_templates(string templateDirectory, string templateName, string expectedResult)
    {
        var generatorSettings = new CSharpGeneratorSettings
        {
            TemplateDirectory = templateDirectory,
        };

        var factory = new DefaultTemplateFactory(generatorSettings, [typeof(CSharpGeneratorSettings).Assembly])
        {
            FluidParser = new LiquidParser(new FluidParserOptions { AllowLiquidTag = true }),
        };
        generatorSettings.TemplateFactory = factory;

        var template = factory.CreateTemplate("csharp", templateName, new { });

        var templateResult = template.Render();

        Assert.Equal(expectedResult, templateResult);
    }
}