//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    // TODO: Add base class for CSharp.EnumTemplateModel and TypeScript.EnumTemplateModel

    /// <summary>The CSharp enum template model.</summary>
    public class EnumTemplateModel
    {
        private readonly JsonSchema _schema;
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="EnumTemplateModel" /> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The settings.</param>
        public EnumTemplateModel(string typeName, JsonSchema schema, CSharpGeneratorSettings settings)
        {
            _schema = schema;
            _settings = settings;
            Name = typeName;
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription => _schema is not JsonSchemaProperty && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string? Description => _schema.Description;

        /// <summary>Gets the property extension data.</summary>
        public IDictionary<string, object?>? ExtensionData => _schema.ExtensionData;

        /// <summary>Gets a value indicating whether the enum is of type string.</summary>
        public bool IsStringEnum => _schema.Type != JsonObjectType.Integer;

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => _settings.TypeAccessModifier;

        /// <summary>Gets or sets if we output as Bit Flags.</summary>
        public bool IsEnumAsBitFlags => _settings.EnforceFlagEnums || _schema.IsFlagEnumerable;
        
        /// <summary>Gets a value indicating whether to use System.Text.Json</summary>
        public bool UseSystemTextJson => _settings.JsonLibrary == CSharpJsonLibrary.SystemTextJson;

        /// <summary>Gets or sets the CSharp JSON library version to use.</summary>
        public decimal JsonLibraryVersion => _settings.JsonLibraryVersion;

        /// <summary>Gets a value indicating whether the enum needs another base type to representing an extended value range.</summary>
        public bool HasExtendedValueRange => _schema.Format == JsonFormatStrings.Long;

        /// <summary>Gets the enum values.</summary>
        public IEnumerable<EnumerationItemModel> Enums
        {
            get
            {
                var entries = new List<EnumerationItemModel>();
                for (var i = 0; i < _schema.Enumeration.Count; i++)
                {
                    var value = _schema.Enumeration.ElementAt(i);
                    if (value != null)
                    {
                        var description = _schema.EnumerationDescriptions.Count > i
                            ? _schema.EnumerationDescriptions[i]
                            : null;

                        if (_schema.Type.IsInteger())
                        {
                            var literal = ValueGeneratorBase.GetIntegerEnumLiteral(value);
                            var name = _schema.EnumerationNames.Count > i
                                ? _schema.EnumerationNames[i]
                                : "_" + (_settings.EnumNameGenerator.GetType() == typeof(DefaultEnumNameGenerator) ? literal : value.ToString());

                            if (_schema.IsFlagEnumerable && long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out long valueInt64))
                            {
                                entries.Add(new EnumerationItemModel
                                {
                                    Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                    OriginalName = name,
                                    Value = value.ToString()!,
                                    Description = description,
                                    InternalValue = valueInt64.ToString(CultureInfo.InvariantCulture),
                                    InternalFlagValue = valueInt64.ToString(CultureInfo.InvariantCulture)
                                });
                            }
                            else
                            {
                                entries.Add(new EnumerationItemModel
                                {
                                    Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                    OriginalName = name,
                                    Value = value.ToString()!,
                                    Description = description,
                                    InternalValue = literal,
                                    InternalFlagValue = (1 << i).ToString(CultureInfo.InvariantCulture)
                                });
                            }
                        }
                        else
                        {
                            var name = _schema.EnumerationNames.Count > i
                                ? _schema.EnumerationNames[i]
                                : value.ToString();

                            entries.Add(new EnumerationItemModel
                            {
                                Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                OriginalName = name!,
                                Value = value.ToString()!,
                                Description = description,
                                InternalValue = i.ToString(CultureInfo.InvariantCulture),
                                InternalFlagValue = (1 << i).ToString(CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }

                return entries;
            }
        }

    }
}
