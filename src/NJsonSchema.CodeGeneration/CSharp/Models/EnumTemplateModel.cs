//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    internal class EnumTemplateModel
    {
        private readonly JsonSchema4 _schema;

        public EnumTemplateModel(string typeName, JsonSchema4 schema)
        {
            _schema = schema; 
            Name = typeName;
        }

        public string Name { get; }

        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        public string Description => _schema.Description;

        public IEnumerable<EnumerationEntry> Enums
        {
            get
            {
                var entries = new List<EnumerationEntry>();
                for (int i = 0; i < _schema.Enumeration.Count; i++)
                {
                    var value = _schema.Enumeration.ElementAt(i);
                    var name = _schema.EnumerationNames.Count > i ?
                        _schema.EnumerationNames.ElementAt(i) :
                        _schema.Type == JsonObjectType.Integer ? "Value" + value : value.ToString();

                    entries.Add(new EnumerationEntry
                    {
                        Value = _schema.Type == JsonObjectType.Integer ? value.ToString() : i.ToString(),
                        Name = ConversionUtilities.ConvertToUpperCamelCase(name)
                    });
                }
                return entries;
            }
        }
    }
}