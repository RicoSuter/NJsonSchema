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
}