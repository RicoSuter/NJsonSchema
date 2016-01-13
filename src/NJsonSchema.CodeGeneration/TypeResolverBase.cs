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
        where TGenerator : TypeGeneratorBase
    {
        private readonly Dictionary<string, TGenerator> _types = new Dictionary<string, TGenerator>();
        private readonly Dictionary<JsonSchema4, string> _generatedTypeNames = new Dictionary<JsonSchema4, string>();

        private int _anonymousClassCount = 0;

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
        public void AddOrReplaceTypeGenerator(string typeName, TGenerator generator)
        {
            _types[typeName] = generator;
        }

        /// <summary>Generates the types (e.g. interfaces, classes, enums, etc).</summary>
        /// <returns>The code.</returns>
        public string GenerateTypes()
        {
            var processedTypes = new List<string>();
            var classes = new Dictionary<string, string>();
            while (_types.Any(t => !processedTypes.Contains(t.Key)))
            {
                foreach (var pair in _types.ToList())
                {
                    processedTypes.Add(pair.Key);
                    var result = pair.Value.GenerateType(pair.Key); 
                    classes[result.TypeName] = result.Code;
                }
            }
            return string.Join("\n\n", classes.Select(p => p.Value));
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the given type usage is required.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public abstract string Resolve(JsonSchema4 schema, bool isRequired, string typeNameHint);

        /// <summary>Creates a type generator.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The generator.</returns>
        protected abstract TGenerator CreateTypeGenerator(JsonSchema4 schema);

        /// <summary>Adds a generator for the given schema if necessary.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns></returns>
        protected virtual string AddGenerator(JsonSchema4 schema, string typeNameHint)
        {
            var typeName = GetOrGenerateTypeName(schema, typeNameHint);
            if (!HasTypeGenerator(typeName))
            {
                var generator = CreateTypeGenerator(schema);
                AddOrReplaceTypeGenerator(typeName, generator);
            }
            return typeName;
        }

        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        protected virtual string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint)
        {
            if (string.IsNullOrEmpty(schema.TypeName))
            {
                if (!_generatedTypeNames.ContainsKey(schema))
                    _generatedTypeNames[schema] = GenerateTypeName(typeNameHint);

                return _generatedTypeNames[schema];
            }

            return schema.TypeName;
        }

        /// <summary>Generates a unique type name with the given hint.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        protected string GenerateTypeName(string typeNameHint)
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