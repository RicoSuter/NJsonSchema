﻿//-----------------------------------------------------------------------
// <copyright file="DefaultTemplateFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Parlot.Fluent;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The default template factory which loads templates from embedded resources.</summary>
    public class DefaultTemplateFactory : ITemplateFactory
    {
        private readonly CodeGeneratorSettingsBase _settings;
        private readonly Assembly[] _assemblies;

        /// <summary>Initializes a new instance of the <see cref="DefaultTemplateFactory"/> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="assemblies">The assemblies containing embedded Liquid templates.</param>
        public DefaultTemplateFactory(CodeGeneratorSettingsBase settings, Assembly[] assemblies)
        {
            _settings = settings;
            _assemblies = assemblies;
        }

        /// <summary>Creates a template for the given language, template name and template model.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <param name="model">The template model.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        public ITemplate CreateTemplate(string language, string template, object model)
        {
            return new LiquidTemplate(
                language,
                template,
                GetLiquidTemplate,
                model,
                GetToolchainVersion(),
                _settings);
        }

        /// <summary>Gets the current toolchain version.</summary>
        /// <returns>The toolchain version.</returns>
        protected virtual string GetToolchainVersion()
        {
            return JsonSchema.ToolchainVersion;
        }

        /// <summary>Gets a Liquid template by name.</summary>
        /// <param name="name">The assembly name.</param>
        /// <returns>The assembly.</returns>
        /// <exception cref="InvalidOperationException">The assembly containting liquid templates could not be found.</exception>
        protected Assembly GetLiquidAssembly(string name)
        {
            var assembly = _assemblies.FirstOrDefault(a => a.FullName.Contains(name));
            if (assembly != null)
            {
                return assembly;
            }

            throw new InvalidOperationException("The assembly '" + name + "' containting liquid templates could not be found.");
        }

        /// <summary>Tries to load an embedded Liquid template.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        protected virtual string GetEmbeddedLiquidTemplate(string language, string template)
        {
            template = template.TrimEnd('!');
            var assembly = GetLiquidAssembly("NJsonSchema.CodeGeneration." + language);
            var resourceName = "NJsonSchema.CodeGeneration." + language + ".Templates." + template + ".liquid";

            var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource != null)
            {
                using var reader = new StreamReader(resource);
                return reader.ReadToEnd();
            }

            throw new InvalidOperationException("Could not load template '" + template + "' for language '" + language + "'.");
        }

        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        private string GetLiquidTemplate(string language, string template, string templateDirectory)
        {
            if (!template.EndsWith('!') && !string.IsNullOrEmpty(templateDirectory))
            {
                var templateFilePath = Path.Combine(templateDirectory, template + ".liquid");
                if (File.Exists(templateFilePath))
                {
                    return File.ReadAllText(templateFilePath);
                }
            }

            return GetEmbeddedLiquidTemplate(language, template);
        }

        private sealed class LiquidTemplate : ITemplate
        {
            internal const string TemplateTagName = "template";
            private static readonly ConcurrentDictionary<(string, string, string), IFluidTemplate> Templates = new ConcurrentDictionary<(string, string, string), IFluidTemplate>();

            static LiquidTemplate()
            {
                // thread-safe
                _parser = new LiquidParser();
                _templateOptions = new TemplateOptions
                {
                    MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                    CultureInfo = CultureInfo.InvariantCulture,
                    Greedy = false
                };
                _templateOptions.Filters.AddFilter("csharpdocs", LiquidFilters.Csharpdocs);
                _templateOptions.Filters.AddFilter("tab", LiquidFilters.Tab);
                _templateOptions.Filters.AddFilter("lowercamelcase", LiquidFilters.Lowercamelcase);
                _templateOptions.Filters.AddFilter("uppercamelcase", LiquidFilters.Uppercamelcase);
                _templateOptions.Filters.AddFilter("literal", LiquidFilters.Literal);
            }

            private readonly string _language;
            private readonly string _template;
            private readonly Func<string, string, string, string> _templateContentLoader;
            private readonly object _model;
            private readonly string _toolchainVersion;
            private readonly CodeGeneratorSettingsBase _settings;

            private static readonly LiquidParser _parser;
            private static readonly TemplateOptions _templateOptions;

            private static readonly Regex _tabCountRegex = new("(\\s*)?\\{%(-)?\\s+template\\s+([a-zA-Z0-9_.]+)(\\s*?.*?)\\s(-)?%}", RegexOptions.Singleline | RegexOptions.Compiled);
            private static readonly Regex _csharpDocsRegex = new("(\n( )*)([^\n]*?) \\| csharpdocs }}", RegexOptions.Singleline | RegexOptions.Compiled);
            private static readonly Regex _tabRegex = new("(\n( )*)([^\n]*?) \\| tab }}", RegexOptions.Singleline | RegexOptions.Compiled);

            public LiquidTemplate(
                string language,
                string template,
                Func<string, string, string, string> templateContentLoader,
                object model,
                string toolchainVersion,
                CodeGeneratorSettingsBase settings)
            {
                _language = language;
                _template = template;
                _templateContentLoader = templateContentLoader;
                _model = model;
                _toolchainVersion = toolchainVersion;
                _settings = settings;
            }

            public string Render()
            {
                var childScope = false;
                TemplateContext? templateContext = null;

                try
                {
                    var templateDirectory = _settings.TemplateDirectory ?? string.Empty;
                    // use language, template name and template directory as key for faster lookup than using the content
                    // template directory as part of key is requred for processing multiple files since files may use different TemplateDirectory for same language
                    var key = (_language, _template, templateDirectory);
                    var template = Templates.GetOrAdd(key, _ =>
                    {
                        // our matching expects unix new lines
                        var templateContent = _templateContentLoader(_language, _template, templateDirectory);
                        var data = templateContent.Replace("\r", "");
                        data = "\n" + data;

                        // tab count parameters to template based on surrounding code, how many spaces before the template tag
                        data = _tabCountRegex.Replace(data,
                            m =>
                            {
                                var whitespace = m.Groups[1].Value;

                                var rewritten = whitespace + "{%" + m.Groups[2].Value + " " + TemplateTagName;
                                // make te parameter a string literal as it's more valid and faster to process
                                rewritten += " '" + m.Groups[3].Value + "' ";

                                if (whitespace.Length > 0 && whitespace[0] == '\n')
                                {
                                    // we can checks how many spaces
                                    var tabCount = whitespace.TrimStart('\n').Length / 4;
                                    rewritten += tabCount + " ";
                                }

                                rewritten += m.Groups[5].Value + "%}";

                                return rewritten;
                            });

                        data = _csharpDocsRegex.Replace(data,  m =>
                            m.Groups[1].Value + m.Groups[3].Value + " | csharpdocs: " + m.Groups[1].Value.Length / 4 + " }}");

                        data = _tabRegex.Replace(data, m =>
                            m.Groups[1].Value + m.Groups[3].Value + " | tab: " + m.Groups[1].Value.Length / 4 + " }}");

                        return _parser.Parse(data);
                    });

                    if (_model is TemplateContext outerContext)
                    {
                        // we came from template call
                        templateContext = outerContext;
                        templateContext.EnterChildScope();
                        childScope = true;
                    }
                    else
                    {
                        templateContext = new TemplateContext(_model, _templateOptions);
                        templateContext.AmbientValues.Add(LiquidParser.SettingsKey, _settings);
                        templateContext.SetValue("ToolchainVersion", _toolchainVersion);
                    }

                    templateContext.AmbientValues[LiquidParser.LanguageKey] = _language;
                    templateContext.AmbientValues[LiquidParser.TemplateKey] = _template;

                    var render = template.Render(templateContext);
                    var trimmed = render.Replace("\r", "").Trim('\n');

                    // clean up cases where we have called template but it produces empty output
                    var withoutEmptyWhiteSpace = Regex.Replace(trimmed, @"^[ ]+__EMPTY-TEMPLATE__$[\n]{0,1}", string.Empty, RegexOptions.Multiline);

                    // just to make sure we don't leak out marker
                    return withoutEmptyWhiteSpace.Replace("__EMPTY-TEMPLATE__", "");
                }
                catch (Exception exception)
                {
                    var message = $"Error while rendering Liquid template {_language}/{_template}: \n{exception.Message}";
                    if (exception.Message.Contains("'{% endif %}' was expected ") && exception.Message.Contains("elseif"))
                    {
                        message += ", did you use 'elseif' instead of correct 'elsif'?";
                    }
                    throw new InvalidOperationException(message, exception);
                }
                finally
                {
                    if (childScope)
                    {
                        templateContext?.ReleaseScope();
                    }
                }
            }
        }

        private static class LiquidFilters
        {
            public static ValueTask<FluidValue> Csharpdocs(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var tabCount = (int) arguments.At(0).ToNumberValue();
                var converted = ConversionUtilities.ConvertCSharpDocs(input.ToStringValue(), tabCount);
                return new ValueTask<FluidValue>(new StringValue(converted));
            }

            public static ValueTask<FluidValue> Tab(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var tabCount = (int) arguments.At(0).ToNumberValue();
                var converted = ConversionUtilities.Tab(input.ToStringValue(), tabCount);
                return new ValueTask<FluidValue>(new StringValue(converted));
            }

            public static ValueTask<FluidValue> Lowercamelcase(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var firstCharacterMustBeAlpha = arguments["firstCharacterMustBeAlpha"].ToBooleanValue();
                var converted = ConversionUtilities.ConvertToLowerCamelCase(input.ToStringValue(), firstCharacterMustBeAlpha);
                return new ValueTask<FluidValue>(new StringValue(converted));
            }

            public static ValueTask<FluidValue> Uppercamelcase(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var firstCharacterMustBeAlpha = arguments["firstCharacterMustBeAlpha"].ToBooleanValue();
                var converted = ConversionUtilities.ConvertToUpperCamelCase(input.ToStringValue(), firstCharacterMustBeAlpha);
                return new ValueTask<FluidValue>(new StringValue(converted));
            }

            public static ValueTask<FluidValue> Literal(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var converted = "\"" + ConversionUtilities.ConvertToStringLiteral(input.ToStringValue()) + "\"";
                return new ValueTask<FluidValue>(new StringValue(converted, encode: false));
            }
        }

        private sealed class LiquidParser : FluidParser
        {
            internal const string LanguageKey = "__language";
            internal const string TemplateKey = "__template";
            internal const string SettingsKey = "__settings";

            public LiquidParser()
            {
                RegisterParserTag(LiquidTemplate.TemplateTagName, Parsers.OneOrMany(Primary), RenderTemplate);
            }

            private static ValueTask<Completion> RenderTemplate(
                IReadOnlyList<Expression> arguments,
                TextWriter writer,
                TextEncoder encoder,
                TemplateContext context)
            {
                var templateName = ((LiteralExpression) arguments[0]).Value.ToStringValue();

                var tabCount = -1;
                if (arguments.Count > 1 && arguments[1] is LiteralExpression literalExpression)
                {
                    tabCount = (int) literalExpression.Value.ToNumberValue();
                }

                var settings = (CodeGeneratorSettingsBase) context.AmbientValues[SettingsKey];
                var language = (string) context.AmbientValues[LanguageKey];
                templateName = !string.IsNullOrEmpty(templateName)
                    ? templateName
                    : (string) context.AmbientValues[TemplateKey] + "!";

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

        /// <summary>
        /// Version that allows all access, safe as models are handled by NJsonSchema.
        /// </summary>
        private sealed class UnsafeMemberAccessStrategy : DefaultMemberAccessStrategy
        {
            private readonly MemberAccessStrategy baseMemberAccessStrategy = new DefaultMemberAccessStrategy();

            public override IMemberAccessor GetAccessor(Type type, string name)
            {
                var accessor = baseMemberAccessStrategy.GetAccessor(type, name);
                if (accessor != null)
                {
                    return accessor;
                }

                baseMemberAccessStrategy.Register(type);
                accessor = baseMemberAccessStrategy.GetAccessor(type, name);
                return accessor;
            }
        }
    }
}
