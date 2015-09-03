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

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        public CSharpTypeResolver()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="knownSchemes">The known schemes.</param>
        public CSharpTypeResolver(JsonSchema4[] knownSchemes)
        {
            foreach (var type in knownSchemes)
                _types[type.TypeName] = new CSharpClassGenerator(type, this);
        }

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
                if (schema.IsTypeReference)
                    return Resolve(schema.TypeReference, isRequired);

                if (!string.IsNullOrEmpty(schema.TypeName))
                {
                    if (!_types.ContainsKey(schema.TypeName))
                    {
                        var generator = new CSharpClassGenerator(schema, this);
                        generator.Namespace = Namespace;
                        _types[schema.TypeName] = generator;
                    }

                    return schema.TypeName;
                }

                return "object";
            }

            throw new NotImplementedException("Type not supported");
        }

        /// <summary>Generates the classes.</summary>
        /// <returns>The code of the generated classes. </returns>
        public string GenerateClasses()
        {
            var classes = "";
            foreach (var type in Types)
                classes += type.GenerateClass() + "\n\n";
            return classes;
        }
    }
}