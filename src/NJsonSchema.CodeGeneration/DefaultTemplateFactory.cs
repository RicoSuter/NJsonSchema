//-----------------------------------------------------------------------
// <copyright file="DefaultTemplateFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using DotLiquid;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

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
            var liquidTemplate = GetLiquidTemplate(language, template);
            return new LiquidTemplate(language, template, liquidTemplate, model, GetToolchainVersion(), _settings);
        }

        /// <summary>Gets the current toolchain version.</summary>
        /// <returns>The toolchain version.</returns>
        protected virtual string GetToolchainVersion()
        {
            return JsonSchema4.ToolchainVersion;
        }

        /// <summary>Gets a Liquid template by name.</summary>
        /// <param name="name">The assembly name.</param>
        /// <returns>The assembly.</returns>
        /// <exception cref="InvalidOperationException">The assembly containting liquid templates could not be found.</exception>
        protected Assembly GetLiquidAssembly(string name)
        {
            var assembly = _assemblies.FirstOrDefault(a => a.FullName.Contains(name));
            if (assembly != null)
                return assembly;

            throw new InvalidOperationException("The assembly '" + name + "' containting liquid templates could not be found.");
        }

        /// <summary>Tries to load an embedded Liquid template.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        protected virtual string GetEmbeddedLiquidTemplate(string language, string template)
        {
            var assembly = GetLiquidAssembly("NJsonSchema.CodeGeneration." + language);
            var resourceName = "NJsonSchema.CodeGeneration." + language + ".Templates." + template + ".liquid";

            var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource != null)
            {
                using (var reader = new StreamReader(resource))
                    return reader.ReadToEnd();
            }

            throw new InvalidOperationException("Could not load template '" + template + "' for language '" + language + "'.");
        }

        /// <exception cref="InvalidOperationException">Could not load template.</exception>
        private string GetLiquidTemplate(string language, string template)
        {
            if (!template.EndsWith("!") && !string.IsNullOrEmpty(_settings.TemplateDirectory))
            {
                var templateFilePath = Path.Combine(_settings.TemplateDirectory, template + ".liquid");
                if (File.Exists(templateFilePath))
                    return File.ReadAllText(templateFilePath);
            }

            return GetEmbeddedLiquidTemplate(language, template);
        }

        internal class LiquidTemplate : ITemplate
        {
            private const string TemplateTagName = "__njs_template";
            private static readonly ConcurrentDictionary<string, Template> Templates = new ConcurrentDictionary<string, Template>();

            static LiquidTemplate()
            {
                Template.RegisterTag<TemplateTag>(TemplateTagName);
            }

            private readonly string _language;
            private readonly string _template;
            private readonly string _data;
            private readonly object _model;
            private readonly string _toolchainVersion;
            private readonly CodeGeneratorSettingsBase _settings;

            public LiquidTemplate(string language, string template, string data, object model, string toolchainVersion, CodeGeneratorSettingsBase settings)
            {
                _language = language;
                _template = template;
                _data = data;
                _model = model;
                _toolchainVersion = toolchainVersion;
                _settings = settings;
            }

            public string Render()
            {
                var hash = _model is Hash ? (Hash)_model : new LiquidProxyHash(_model);
                hash[TemplateTag.LanguageKey] = _language;
                hash[TemplateTag.TemplateKey] = _template;
                hash[TemplateTag.SettingsKey] = _settings;
                hash["ToolchainVersion"] = _toolchainVersion;

                if (!Templates.ContainsKey(_data))
                {
                    var data = Regex.Replace("\n" + _data, "(\n( )*?)\\{% template (.*?) %}", m =>
                            "\n{%- " + TemplateTagName + " " + m.Groups[3].Value + " " + m.Groups[1].Value.Length / 4 + " -%}",
                        RegexOptions.Singleline).Trim();

                    data = Regex.Replace("\n" + data, "\\{% template (.*?) %}", m =>
                            "{% " + TemplateTagName + " " + m.Groups[1].Value + " -1 %}",
                        RegexOptions.Singleline).Trim();

                    data = data.Replace("{% template %}", "{% " + TemplateTagName + " %}");

                    data = Regex.Replace(data, "(\n( )*)([^\n]*?) \\| csharpdocs }}", m =>
                        m.Groups[1].Value + m.Groups[3].Value + " | csharpdocs: " + m.Groups[1].Value.Length / 4 + " }}",
                        RegexOptions.Singleline);

                    data = Regex.Replace(data, "(\n( )*)([^\n]*?) \\| tab }}", m =>
                        m.Groups[1].Value + m.Groups[3].Value + " | tab: " + m.Groups[1].Value.Length / 4 + " }}",
                        RegexOptions.Singleline);

                    Templates[_data] = Template.Parse(data);
                }

                var template = Templates[_data];
                return template.Render(new RenderParameters(CultureInfo.InvariantCulture)
                {
                    LocalVariables = hash,
                    Filters = new[] { typeof(LiquidFilters) }
                }).Replace("\r", "").Trim();
            }
        }

        internal static class LiquidFilters
        {
            public static string Csharpdocs(string input, int tabCount)
            {
                return ConversionUtilities.ConvertCSharpDocBreaks(input, tabCount);
            }

            public static string Tab(Context context, string input, int tabCount)
            {
                return ConversionUtilities.Tab(input, tabCount);
            }

            public static string Lowercamelcase(Context context, string input, bool firstCharacterMustBeAlpha = true)
            {
                return ConversionUtilities.ConvertToLowerCamelCase(input, firstCharacterMustBeAlpha);
            }

            public static string Uppercamelcase(Context context, string input, bool firstCharacterMustBeAlpha = true)
            {
                return ConversionUtilities.ConvertToUpperCamelCase(input, firstCharacterMustBeAlpha);
            }

            public static IEnumerable<object> Concat(Context context, IEnumerable<object> input, IEnumerable<object> concat)
            {
                return input.Concat(concat ?? Enumerable.Empty<object>()).ToList();
            }

            public static IEnumerable<object> Empty(Context context, object input)
            {
                return Enumerable.Empty<object>();
            }
        }

        internal class TemplateTag : Tag
        {
            public static string LanguageKey = "__language";
            public static string TemplateKey = "__template";
            public static string SettingsKey = "__settings";

            private string _template;
            private int _tabCount;

            public override void Initialize(string tagName, string markup, List<string> tokens)
            {
                var parts = markup.Trim().Split(' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                _template = parts.Length >= 1 ? parts[0] : null;
                _tabCount = parts.Length >= 2 ? int.Parse(parts[1]) : 0;
                base.Initialize(tagName, markup, tokens);
            }

            public override void Render(Context context, TextWriter result)
            {
                try
                {
                    var model = CreateModelWithParentContext(context);

                    var rootContext = context.Environments[0];
                    var settings = (CodeGeneratorSettingsBase)rootContext[SettingsKey];
                    var language = (string)rootContext[LanguageKey];
                    var templateName = !string.IsNullOrEmpty(_template) ? _template : (string)rootContext[TemplateKey] + "!";

                    var template = settings.TemplateFactory.CreateTemplate(language, templateName, model);
                    var output = template.Render().Trim();

                    if (string.IsNullOrEmpty(output))
                        result.Write("");
                    else if (_tabCount >= 0)
                    {
                        result.Write(string.Join("", Enumerable.Repeat("    ", _tabCount)) +
                            ConversionUtilities.Tab(output, _tabCount) + "\r\n");
                    }
                    else
                        result.Write(output);
                }
                catch (InvalidOperationException)
                {
                }
            }

            private LiquidProxyHash CreateModelWithParentContext(Context context)
            {
                var model = new LiquidProxyHash(((LiquidProxyHash)context.Environments[0]).Object);
                model.Merge(context.Registers);
                foreach (var scope in Enumerable.Reverse(context.Scopes))
                    model.Merge(scope);
                foreach (var environment in Enumerable.Reverse(context.Environments))
                    model.Merge(environment);
                return model;
            }
        }
    }
}
