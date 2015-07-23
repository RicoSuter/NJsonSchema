//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class CSharpTypeResolver
    {
        private readonly Dictionary<string, CSharpClassGenerator> _types = new Dictionary<string, CSharpClassGenerator>();

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Gets the available types.</summary>
        public IReadOnlyCollection<CSharpClassGenerator> Types
        {
            get { return _types.Values.ToList().AsReadOnly(); }
        }

        /// <summary>Resolves the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the type is required.</param>
        /// <returns>The CSharp type name. </returns>
        public string Resolve(JsonSchema4 schema, bool isRequired)
        {
            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = schema;
                if (property.Item != null)
                    return string.Format("ObservableCollection<{0}>", Resolve(property.Item, true));

                throw new NotImplementedException("Items not supported");
            }

            if (type.HasFlag(JsonObjectType.Number))
                return isRequired ? "decimal" : "decimal?";

            if (type.HasFlag(JsonObjectType.Integer))
                return isRequired ? "long" : "long?";

            if (type.HasFlag(JsonObjectType.Boolean))
                return isRequired ? "bool" : "bool?";

            if (type.HasFlag(JsonObjectType.String))
            {
                if (schema.Format == JsonFormatStrings.DateTime)
                    return isRequired ? "DateTime" : "DateTime?";
                else
                    return "string";
            }

            if (type.HasFlag(JsonObjectType.Object))
            {
                if (!string.IsNullOrEmpty(schema.Title))
                {
                    if (!_types.ContainsKey(schema.Title))
                    {
                        var generator = new CSharpClassGenerator(schema, this);
                        generator.Namespace = Namespace;
                        _types[schema.Title] = generator;
                    }

                    return schema.Title;
                }
                return "object";
            }

            throw new NotImplementedException("Type not supported");
        }
    }
}