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
        /// <param name="package">The package name (i.e. language).</param>
        /// <param name="template">The template name.</param>
        /// <param name="model">The template model.</param>
        /// <returns>The template.</returns>
        /// <exception cref="InvalidOperationException">Could not load template..</exception>
        public virtual ITemplate CreateTemplate(string package, string template, object model)
        {
            if (_settings.UseLiquidTemplates)
            {
                var assembly = Assembly.Load(new AssemblyName("NJsonSchema.CodeGeneration." + package));
                var resourceName = "NJsonSchema.CodeGeneration." + package + ".Templates." + template + ".liquid";

                var resource = assembly.GetManifestResourceStream(resourceName);
                if (resource != null)
                {
                    using (var reader = new StreamReader(resource))
                        return new LiquidTemplate(reader.ReadToEnd(), model);
                }
                else
                {
                    return CreateT4Template(package, template, model);
                }
            }
            else
            {
                return CreateT4Template(package, template, model);
            }
        }

        private ITemplate CreateT4Template(string package, string template, object model)
        {
            var typeName = "NJsonSchema.CodeGeneration." + package + ".Templates." + template + "Template";
            var type = Type.GetType(typeName);
            if (type == null)
                type = Assembly.Load(new AssemblyName("NJsonSchema.CodeGeneration." + package))?.GetType(typeName);

            if (type != null)
                return (ITemplate) Activator.CreateInstance(type, model);

            throw new InvalidOperationException("Could not load template '" + template + "'.");
        }
    }
}