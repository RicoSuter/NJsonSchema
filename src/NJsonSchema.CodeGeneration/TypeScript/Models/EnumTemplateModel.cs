//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
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

        public List<EnumerationEntry> Enums => GetEnumeration(_schema);

        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        public string Description => ConversionUtilities.RemoveLineBreaks(_schema.Description);

        private List<EnumerationEntry> GetEnumeration(JsonSchema4 schema)
        {
            var entries = new List<EnumerationEntry>();
            for (int i = 0; i < schema.Enumeration.Count; i++)
            {
                var value = schema.Enumeration.ElementAt(i);
                var name = schema.EnumerationNames.Count > i ?
                    schema.EnumerationNames.ElementAt(i) :
                    schema.Type == JsonObjectType.Integer ? "Value" + value : value.ToString();

                entries.Add(new EnumerationEntry
                {
                    Value = schema.Type == JsonObjectType.Integer ? value.ToString() : "<any>\"" + value + "\"",
                    Name = ConversionUtilities.ConvertToUpperCamelCase(name)
                });
            }
            return entries;
        }
    }
}