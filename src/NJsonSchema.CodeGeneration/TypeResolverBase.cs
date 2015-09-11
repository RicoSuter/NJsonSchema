//-----------------------------------------------------------------------
// <copyright file="TypeResolverBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type resolver base.</summary>
    public abstract class TypeResolverBase<TGenerator> 
        where TGenerator : GeneratorBase
    {
        private readonly Dictionary<string, TGenerator> _types = new Dictionary<string, TGenerator>();
        private int _anonymousClassCount = 0;

        /// <summary>Gets the available types.</summary>
        public IReadOnlyCollection<TGenerator> Types
        {
            get { return _types.Values.ToList().AsReadOnly(); }
        }

        /// <summary>Determines whether the generator for a given type name is registered.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public bool HasTypeGenerator(string typeName)
        {
            return _types.ContainsKey(typeName);
        }

        /// <summary>Adds the type generator for a given type name.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="generator">The generator.</param>
        public void AddTypeGenerator(string typeName, TGenerator generator)
        {
            _types.Add(typeName, generator);
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the given type usage is required.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public abstract string Resolve(JsonSchema4 schema, bool isRequired, string typeNameHint);

        /// <summary>Gets the name of the type of the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        protected string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(schema.TypeName))
                return GenerateTypeName(typeNameHint);
            return schema.TypeName; 
        }

        private string GenerateTypeName(string typeNameHint)
        {
            if (!string.IsNullOrEmpty(typeNameHint))
            {
                if (!HasTypeGenerator(typeNameHint))
                    return typeNameHint;

                do
                {
                    _anonymousClassCount++;
                } while (HasTypeGenerator(typeNameHint + _anonymousClassCount));

                return typeNameHint + _anonymousClassCount;
            }
            else
                return GenerateTypeName("Anonymous");
        }
    }
}