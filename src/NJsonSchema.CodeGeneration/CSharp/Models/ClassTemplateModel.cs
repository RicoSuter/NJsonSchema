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
    internal class ClassTemplateModel
    {
        private readonly CSharpTypeResolver _resolver;
        private readonly JsonSchema4 _schema;
        private readonly CSharpGeneratorSettings _settings;

        public ClassTemplateModel(string typeName, CSharpGeneratorSettings settings, CSharpTypeResolver resolver, JsonSchema4 schema, IEnumerable<PropertyModel> properties)
        {
            _resolver = resolver;
            _schema = schema;
            _settings = settings; 

            Class = typeName;
            Properties = properties;
        }

        public string Class { get; set; }

        public string Namespace => _settings.Namespace;

        public IEnumerable<PropertyModel> Properties { get; }

        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        public string Description => _schema.Description;

        public bool Inpc => _settings.ClassStyle == CSharpClassStyle.Inpc;

        public bool HasInheritance => _schema.InheritedSchemas.Count >= 1;

        public string BaseClass => HasInheritance ? _resolver.Resolve(_schema.InheritedSchemas.First(), false, string.Empty) : null;

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