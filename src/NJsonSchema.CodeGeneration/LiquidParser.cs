using System.Text.Encodings.Web;
using Fluid;
using Fluid.Ast;
using Parlot.Fluent;

namespace NJsonSchema.CodeGeneration;

/// <summary>
/// A custom <see cref="FluidParser"/> implementation that can handle NJsonSchema liquid templating language extensions and customizations.
/// </summary>
public sealed class LiquidParser : FluidParser
{
    internal const string LanguageKey = "__language";
    internal const string TemplateKey = "__template";
    internal const string SettingsKey = "__settings";

    /// <summary>
    /// Creates a new instance of custom Liquid parser that can handle NJsonSchema templates.
    /// </summary>
    /// <param name="options"></param>
    public LiquidParser(FluidParserOptions options) : base(options)
    {
        RegisterParserTag(DefaultTemplateFactory.TemplateTagName, Parsers.OneOrMany(Primary), RenderTemplate);
    }

    private static ValueTask<Completion> RenderTemplate(
        IReadOnlyList<Expression> arguments,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        var templateName = ((LiteralExpression)arguments[0]).Value.ToStringValue();

        var tabCount = -1;
        if (arguments.Count > 1 && arguments[1] is LiteralExpression literalExpression)
        {
            tabCount = (int)literalExpression.Value.ToNumberValue();
        }

        var settings = (CodeGeneratorSettingsBase)context.AmbientValues[SettingsKey];
        var language = (string)context.AmbientValues[LanguageKey];
        templateName = !string.IsNullOrEmpty(templateName)
            ? templateName
            : (string)context.AmbientValues[TemplateKey] + "!";

        var template = settings.TemplateFactory.CreateTemplate(language, templateName, context);
        var output = template.Render();

        if (string.IsNullOrWhiteSpace(output))
        {
            // signal cleanup
            writer.Write("__EMPTY-TEMPLATE__");
        }
        else if (tabCount > 0)
        {
            ConversionUtilities.Tab(output, tabCount, writer);
        }
        else
        {
            writer.Write(output);
        }

        return new ValueTask<Completion>(Completion.Normal);
    }
}