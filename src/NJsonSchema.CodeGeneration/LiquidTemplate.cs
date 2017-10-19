//-----------------------------------------------------------------------
// <copyright file="LiquidTemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DotLiquid;

namespace NJsonSchema.CodeGeneration
{
    internal class LiquidTemplate : ITemplate
    {
        private readonly string _language;
        private readonly string _template;
        private readonly string _data;
        private readonly object _model;
        private readonly CodeGeneratorSettingsBase _settings;

        public LiquidTemplate(string language, string template, string data, object model, CodeGeneratorSettingsBase settings)
        {
            _language = language;
            _template = template;
            _data = data;
            _model = model;
            _settings = settings;
        }

        public string Render()
        {
            Template.RegisterTag<TemplateTag>("template");

            var data = Regex.Replace(_data, "(\n( )*?)\\{% (template .*?) %}", m =>
                "\n{%- " + m.Groups[3].Value + " " + m.Groups[1].Value.Length / 4 + " -%}",
                RegexOptions.Singleline);

            var template = Template.Parse(data);

            var hash = _model is Hash ? (Hash)_model : LiquidHash.FromObject(_model);
            hash[TemplateTag.LanguageKey] = _language;
            hash[TemplateTag.TemplateKey] = _template;
            hash[TemplateTag.SettingsKey] = _settings;

            if (!hash.ContainsKey("ToolchainVersion"))
                hash["ToolchainVersion"] = JsonSchema4.ToolchainVersion;

            return template.Render(new RenderParameters
            {
                LocalVariables = hash,
                Filters = new[] { typeof(LiquidFilters) }
            });
        }
    }

    internal static class LiquidFilters
    {
        public static string CSharpDocs(string input)
        {
            return ConversionUtilities.ConvertCSharpDocBreaks(input, 0);
        }

        public static string Tab(Context context, string input, int tabCount)
        {
            return ConversionUtilities.Tab(input, tabCount);
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
            var parts = markup.Trim().Split(' ');
            _template = parts[0];
            _tabCount = parts.Length >= 2 ? int.Parse(parts[1]) : 0;
            base.Initialize(tagName, markup, tokens);
        }

        public override void Render(Context context, TextWriter result)
        {
            try
            {
                var hash = new Hash();
                foreach (var environment in context.Environments)
                    hash.Merge(environment);

                var settings = (CodeGeneratorSettingsBase)hash[SettingsKey];
                var template = settings.TemplateFactory.CreateTemplate(
                    (string)hash[LanguageKey],
                    !string.IsNullOrEmpty(_template) ? (string)hash[TemplateKey] + "." + _template : (string)hash[TemplateKey] + "!",
                    hash);

                var output = template.Render();

                if (string.IsNullOrEmpty(output))
                    result.Write("");
                else
                {
                    result.Write(string.Join("", Enumerable.Repeat("    ", _tabCount)) + 
                        ConversionUtilities.Tab(output, _tabCount) + "\r\n");
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}