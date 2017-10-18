//-----------------------------------------------------------------------
// <copyright file="DefaultTemplateFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The default template factory which loads templates from embedded resources.</summary>
    public class DefaultTemplateFactory : ITemplateFactory
    {
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="DefaultTemplateFactory"/> class.</summary>
        /// <param name="settings">The settings.</param>
        public DefaultTemplateFactory(CodeGeneratorSettingsBase settings)
        {
            _settings = settings;
        }

        /// <summary>Creates a template for the given language, template name and template model.</summary>
        /// <remarks>Supports NJsonSchema and NSwag embedded templates.</remarks>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <param name="model">The template model.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template..</exception>
        public virtual ITemplate CreateTemplate(string language, string template, object model)
        {
            var liquidTemplate = TryGetLiquidTemplate(language, template);
            if (liquidTemplate != null)
                return new LiquidTemplate(language, template, liquidTemplate, model, _settings);
            else
                return CreateT4Template(language, template, model);
        }

        /// <summary>Tries to load a Liquid template.</summary>
        /// <param name="language">The language.</param>
        /// <param name="template">The template name.</param>
        /// <returns>The template.</returns>
        protected virtual string TryGetLiquidTemplate(string language, string template)
        {
            if (_settings.UseLiquidTemplates)
            {
                if (!template.EndsWith("!") &&
                    !string.IsNullOrEmpty(_settings.TemplateDirectory) &&
                    Directory.Exists(_settings.TemplateDirectory))
                {
                    var templateFilePath = Path.Combine(_settings.TemplateDirectory, template + ".liquid");
                    if (File.Exists(templateFilePath))
                        return File.ReadAllText(templateFilePath);
                }

                return TryLoadEmbeddedLiquidTemplate(language, template.TrimEnd('!'));
            }

            return null;
        }
        
        private string TryLoadEmbeddedLiquidTemplate(string language, string template)
        {
            var assembly = Assembly.Load(new AssemblyName("NJsonSchema.CodeGeneration." + language));
            var resourceName = "NJsonSchema.CodeGeneration." + language + ".Templates.Liquid." + template + ".liquid";

            var resource = assembly.GetManifestResourceStream(resourceName);
            if (resource != null)
            {
                using (var reader = new StreamReader(resource))
                    return reader.ReadToEnd();
            }

            return null;
        }

        /// <exception cref="InvalidOperationException">Could not load template..</exception>
        private ITemplate CreateT4Template(string language, string template, object model)
        {
            var typeName = "NJsonSchema.CodeGeneration." + language + ".Templates." + template + "Template";
            var type = Type.GetType(typeName);
            if (type == null)
                type = Assembly.Load(new AssemblyName("NJsonSchema.CodeGeneration." + language))?.GetType(typeName);

            if (type != null)
                return (ITemplate)Activator.CreateInstance(type, model);

            throw new InvalidOperationException("Could not load template '" + template + "' for language '" + language + "'.");
        }
    }
}