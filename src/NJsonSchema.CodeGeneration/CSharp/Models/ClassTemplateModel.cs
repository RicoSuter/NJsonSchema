//-----------------------------------------------------------------------
// <copyright file="ClassTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The CSharp class template model.</summary>
    public class ClassTemplateModel
    {
        private readonly CSharpTypeResolver _resolver;
        private readonly JsonSchema4 _schema;
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModel"/> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="properties">The properties.</param>
        public ClassTemplateModel(string typeName, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, JsonSchema4 schema, IEnumerable<PropertyModel> properties)
        {
            _resolver = resolver;
            _schema = schema;
            _settings = settings;

            Class = typeName;
            Properties = properties;
        }

        /// <summary>Gets or sets the class name.</summary>
        public string Class { get; set; }

        /// <summary>Gets the namespace.</summary>
        public string Namespace => _settings.Namespace;

        /// <summary>Gets the property models.</summary>
        public IEnumerable<PropertyModel> Properties { get; }

        /// <summary>Gets a value indicating whether the class has description.</summary>
        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _schema.Description;

        /// <summary>Gets a value indicating whether the class style is INPC.</summary>
        /// <value><c>true</c> if inpc; otherwise, <c>false</c>.</value>
        public bool Inpc => _settings.ClassStyle == CSharpClassStyle.Inpc;

        /// <summary>Gets a value indicating whether the class has discriminator property.</summary>
        public bool HasDiscriminator => !string.IsNullOrEmpty(_schema.Discriminator);

        /// <summary>Gets the discriminator property name.</summary>
        public string Discriminator => _schema.Discriminator;

        /// <summary>Gets a value indicating whether the class has a parent class.</summary>
        public bool HasInheritance => _schema.InheritedSchemas.Count >= 1;

        /// <summary>Gets the base class name.</summary>
        public string BaseClass => HasInheritance ? _resolver.Resolve(_schema.InheritedSchemas.First(), false, string.Empty) : null;

        /// <summary>Gets the inheritance code.</summary>
        public string Inheritance
        {
            get
            {
                if (HasInheritance)
                    return ": " + BaseClass + (_settings.ClassStyle == CSharpClassStyle.Inpc ? ", INotifyPropertyChanged" : "");
                else
                    return _settings.ClassStyle == CSharpClassStyle.Inpc ? ": INotifyPropertyChanged" : "";
            }
        }
    }
}