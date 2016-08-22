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
    /// <summary>The CSharp enum template model.</summary>
    public class EnumTemplateModel
    {
        private readonly JsonSchema4 _schema;

        /// <summary>Initializes a new instance of the <see cref="EnumTemplateModel"/> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="schema">The schema.</param>
        public EnumTemplateModel(string typeName, JsonSchema4 schema)
        {
            _schema = schema;
            Name = typeName;
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _schema.Description;

        /// <summary>Gets a value indicating whether the enum is of type string.</summary>
        public bool IsStringEnum => _schema.Type != JsonObjectType.Integer;

        /// <summary>Gets the enum values.</summary>
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
                        _schema.Type == JsonObjectType.Integer ? "_" + value : value.ToString();

                    entries.Add(new EnumerationEntry
                    {
                        InternalValue = _schema.Type == JsonObjectType.Integer ? value.ToString() : i.ToString(),
                        Value = value.ToString(),
                        Name = ConversionUtilities.ConvertToUpperCamelCase(name, true)
                    });
                }
                return entries;
            }
        }
    }
}