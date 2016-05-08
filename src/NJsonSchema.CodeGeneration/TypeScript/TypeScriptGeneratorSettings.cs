//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.TypeScript.Templates;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The generator settings.</summary>
    public class TypeScriptGeneratorSettings
    {
        /// <summary>Initializes a new instance of the <see cref="TypeScriptGeneratorSettings"/> class.</summary>
        public TypeScriptGeneratorSettings()
        {
            GenerateReadOnlyKeywords = true;
            TypeStyle = TypeScriptTypeStyle.Interface;
            DerivedTypeMappings = new List<TypeScriptDerivedTypeMapping>();
        }

        /// <summary>Gets or sets a value indicating whether to generate the readonly keywords (only available in TS 2.0+, default: true).</summary>
        public bool GenerateReadOnlyKeywords { get; set; }

        /// <summary>Gets or sets the type style (experimental, default: Interface).</summary>
        public TypeScriptTypeStyle TypeStyle { get; set; }

        /// <summary>Gets or sets the derived type mappings (experimental).</summary>
        public IList<TypeScriptDerivedTypeMapping> DerivedTypeMappings { get; private set; }

        /// <summary>Maps a type to the new type or returns the original type if no mapping is available.</summary>
        /// <param name="type">The type name.</param>
        /// <returns>The new type name.</returns>
        public string GetMappedDerivedType(string type)
        {
            var mapping = DerivedTypeMappings.SingleOrDefault(m => m.Type == type);
            if (mapping != null)
                return $"{GetModuleAlias(mapping)}.{mapping.NewType}";

            return type; 
        }

        private string GetModuleAlias(TypeScriptDerivedTypeMapping mapping)
        {
            var modules = DerivedTypeMappings.GroupBy(m => m.Module).Select(g => g.Key).ToList();
            return "m" + modules.IndexOf(mapping.Module);
        }

        internal ITemplate CreateTemplate()
        {
            if (TypeStyle == TypeScriptTypeStyle.Interface)
                return new InterfaceTemplate();

            if (TypeStyle == TypeScriptTypeStyle.Class)
                return new ClassTemplate();

            if (TypeStyle == TypeScriptTypeStyle.KoObservableClass)
                return new KnockoutClassTemplate();

            throw new NotImplementedException();
        }
    }
}