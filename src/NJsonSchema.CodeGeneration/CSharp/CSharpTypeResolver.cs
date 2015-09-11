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
    public class CSharpTypeResolver : TypeResolverBase<CSharpClassGenerator>
    {

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        public CSharpTypeResolver()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="knownSchemes">The known schemes.</param>
        public CSharpTypeResolver(JsonSchema4[] knownSchemes)
        {
            foreach (var type in knownSchemes)
                AddTypeGenerator(type.TypeName, new CSharpClassGenerator(type.ActualSchema, this));
        }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the given type usage is required.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public override string Resolve(JsonSchema4 schema, bool isRequired, string typeNameHint)
        {
            schema = schema.ActualSchema;

            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = schema;
                if (property.Item != null)
                    return string.Format("ObservableCollection<{0}>", Resolve(property.Item, true, null));

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
                if (schema.IsDictionary)
                    return string.Format("Dictionary<string, {0}>", Resolve(schema.AdditionalPropertiesSchema, true, null));
                
                var typeName = GetOrGenerateTypeName(schema, typeNameHint);
                if (!HasTypeGenerator(typeName))
                {
                    var generator = new CSharpClassGenerator(schema, this);
                    generator.Namespace = Namespace;
                    AddTypeGenerator(typeName, generator);
                }
                return typeName;
            }

            throw new NotImplementedException("Type not supported");
        }

        /// <summary>Generates the classes.</summary>
        /// <returns>The code of the generated classes. </returns>
        public string GenerateClasses()
        {
            var classes = "";
            var runGenerators = new List<CSharpClassGenerator>();
            while (Types.Any(t => !runGenerators.Contains(t)))
                classes += GenerateClasses(runGenerators);
            return classes;
        }

        private string GenerateClasses(List<CSharpClassGenerator> runGenerators)
        {
            var classes = "";
            foreach (var type in Types.Where(t => !runGenerators.Contains(t)))
            {
                classes += type.GenerateClass() + "\n\n";
                runGenerators.Add(type);
            }
            return classes;
        }
    }
}