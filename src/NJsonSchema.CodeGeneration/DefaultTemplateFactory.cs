//-----------------------------------------------------------------------
// <copyright file="DefaultTemplateFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fluid;
using Fluid.Values;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The default template factory which loads templates from embedded resources.</summary>
    public partial class DefaultTemplateFactory : ITemplateFactory
    {
        internal const string TemplateTagName = "template";

        private readonly CodeGeneratorSettingsBase _settings;
        private readonly Assembly[] _assemblies;
        private readonly Func<string, string, string, string> _templateContentLoader;

        /// <summary>Initializes a new instance of the <see cref="DefaultTemplateFactory"/> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="assemblies">The assemblies containing embedded Liquid templates.</param>
        public DefaultTemplateFactory(CodeGeneratorSettingsBase settings, Assembly[] assemblies)
        {
            _settings = settings;
            _assemblies = assemblies;
            _templateContentLoader = GetLiquidTemplate;

            FluidParser = new LiquidParser(new FluidParserOptions());

            var templateOptions = new TemplateOptions
            {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                CultureInfo = CultureInfo.InvariantCulture,
                Greedy = false
            };
            templateOptions.Filters.AddFilter("csharpdocs", LiquidFilters.Csharpdocs);
            templateOptions.Filters.AddFilter("tab", LiquidFilters.Tab);
            templateOptions.Filters.AddFilter("lowercamelcase", LiquidFilters.Lowercamelcase);
            templateOptions.Filters.AddFilter("uppercamelcase", LiquidFilters.Uppercamelcase);
            templateOptions.Filters.AddFilter("literal", LiquidFilters.Literal);

            TemplateOptions = templateOptions;
        }

        /// <summary>Creates a template for the given language, template name and template model.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <param name="model">The template model.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        public ITemplate CreateTemplate(string language, string template, object model)
        {
            return new LiquidTemplate(FluidParser, TemplateOptions, language, template, _templateContentLoader, model, GetToolchainVersion(), _settings);
        }

        /// <summary>
        /// Gets or sets the <see cref="FluidParser"/> that will be used when parsing template content.
        /// </summary>
        public FluidParser FluidParser { get; set; }

        /// <summary>
        /// Gets the <see cref="TemplateOptions"/> that will be used when processing template content.
        /// </summary>
        /// <value></value>
        public TemplateOptions TemplateOptions { get; }

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
            var assembly = _assemblies.FirstOrDefault(a => a.FullName?.Contains(name) == true);
            if (assembly != null)
            {
                return assembly;
            }

            throw new InvalidOperationException("The assembly '" + name + "' containing liquid templates could not be found.");
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
                foreach (var directory in templateDirectory.Split([';'], StringSplitOptions.RemoveEmptyEntries))
                {
                    var templateFilePath = Path.Combine(directory, template + ".liquid");
                    if (File.Exists(templateFilePath))
                    {
                        return File.ReadAllText(templateFilePath);
                    }
                }
            }

            return GetEmbeddedLiquidTemplate(language, template);
        }

        private sealed partial class LiquidTemplate : ITemplate
        {
            private readonly record struct TemplateKey(string Language, string Template, string TemplateDirectory);

            private static readonly ConcurrentDictionary<TemplateKey, IFluidTemplate> Templates = new();

            private readonly FluidParser _parser;
            private readonly TemplateOptions _templateOptions;

            private readonly string _language;
            private readonly string _template;
            private readonly Func<string, string, string, string> _templateContentLoader;
            private readonly object _model;
            private readonly string _toolchainVersion;
            private readonly CodeGeneratorSettingsBase _settings;

            private const string TabCountRegexString = @"(\s*)?\{%(-)?\s+template\s+([a-zA-Z0-9_.]+)(\s*?.*?)\s(-)?%}";
            private const string CsharpDocsRegexString = "(\n( )*)([^\n]*?) \\| csharpdocs }}";
            private const string TabRegexString = "(\n( )*)([^\n]*?) \\| tab }}";
            private const string EmptyTemplateCleanupRegexString = @"^[ ]+__EMPTY-TEMPLATE__$[\n]{0,1}";

#if NET8_0_OR_GREATER
            private static readonly Regex _tabCountRegex = TabCountRegex();
            private static readonly Regex _csharpDocsRegex = CsharpDocsRegex();
            private static readonly Regex _tabRegex = TabRegex();
            private static readonly Regex _emptyTemplateCleanupRegex = EmptyTemplateCleanupRegex();
#else
            private static readonly Regex _tabCountRegex = new(TabCountRegexString, RegexOptions.Singleline | RegexOptions.Compiled);
            private static readonly Regex _csharpDocsRegex = new(CsharpDocsRegexString, RegexOptions.Singleline | RegexOptions.Compiled);
            private static readonly Regex _tabRegex = new(TabRegexString, RegexOptions.Singleline | RegexOptions.Compiled);
            private static readonly Regex _emptyTemplateCleanupRegex = new(EmptyTemplateCleanupRegexString, RegexOptions.Multiline | RegexOptions.Compiled);
#endif

            public LiquidTemplate(
                FluidParser parser,
                TemplateOptions templateOptions,
                string language,
                string template,
                Func<string, string, string, string> templateContentLoader,
                object model,
                string toolchainVersion,
                CodeGeneratorSettingsBase settings)
            {
                _parser = parser;
                _templateOptions = templateOptions;
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
                    var key = new TemplateKey(_language, _template, templateDirectory);
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

                        data = _csharpDocsRegex.Replace(data, m =>
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
                    var withoutEmptyWhiteSpace = _emptyTemplateCleanupRegex.Replace(trimmed, string.Empty);

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

#if NET8_0_OR_GREATER
            [GeneratedRegex(TabCountRegexString, RegexOptions.Compiled | RegexOptions.Singleline)]
            private static partial Regex TabCountRegex();

            [GeneratedRegex(CsharpDocsRegexString, RegexOptions.Compiled | RegexOptions.Singleline)]
            private static partial Regex CsharpDocsRegex();

            [GeneratedRegex(TabRegexString, RegexOptions.Compiled | RegexOptions.Singleline)]
            private static partial Regex TabRegex();

            [GeneratedRegex(EmptyTemplateCleanupRegexString, RegexOptions.Compiled | RegexOptions.Multiline)]
            private static partial Regex EmptyTemplateCleanupRegex();
#endif
        }

        private static class LiquidFilters
        {
            public static ValueTask<FluidValue> Csharpdocs(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var tabCount = (int)arguments.At(0).ToNumberValue();
                var converted = ConversionUtilities.ConvertCSharpDocs(input.ToStringValue(), tabCount);
                return new ValueTask<FluidValue>(new StringValue(converted));
            }

            public static ValueTask<FluidValue> Tab(FluidValue input, FilterArguments arguments, TemplateContext context)
            {
                var tabCount = (int)arguments.At(0).ToNumberValue();
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
                var converted = ConversionUtilities.ConvertToStringLiteral(input.ToStringValue(), "\"", "\"");
                return new ValueTask<FluidValue>(new StringValue(converted, encode: false));
            }
        }

        /// <summary>
        /// Version that allows all access, safe as models are handled by NJsonSchema.
        /// </summary>
        private sealed class UnsafeMemberAccessStrategy : DefaultMemberAccessStrategy
        {
            private readonly ConcurrentDictionary<Type, object?> _handledTypes = new();

            private readonly MemberAccessStrategy baseMemberAccessStrategy = new DefaultMemberAccessStrategy();

            public override IMemberAccessor GetAccessor(Type type, string name)
            {
                var accessor = baseMemberAccessStrategy.GetAccessor(type, name);
                if (accessor != null)
                {
                    return accessor;
                }

                if (!_handledTypes.ContainsKey(type))
                {
                    baseMemberAccessStrategy.Register(type);
                    _handledTypes.TryAdd(type, null);
                }

                accessor = baseMemberAccessStrategy.GetAccessor(type, name);

                return accessor;
            }
        }
    }
}