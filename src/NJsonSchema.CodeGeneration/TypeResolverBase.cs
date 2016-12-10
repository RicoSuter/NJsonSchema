//-----------------------------------------------------------------------
// <copyright file="TypeResolverBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.Infrastructure;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type resolver base.</summary>
    public abstract class TypeResolverBase<TGenerator> : ITypeResolver
        where TGenerator : TypeGeneratorBase
    {
        private readonly Dictionary<string, TGenerator> _types = new Dictionary<string, TGenerator>();
        private readonly Dictionary<JsonSchema4, string> _generatedTypeNames = new Dictionary<JsonSchema4, string>();
        private readonly ITypeNameGenerator _typeNameGenerator;

        /// <summary>Initializes a new instance of the <see cref="TypeResolverBase{TGenerator}"/> class.</summary>
        /// <param name="typeNameGenerator">The type name generator.</param>
        protected TypeResolverBase(ITypeNameGenerator typeNameGenerator)
        {
            _typeNameGenerator = typeNameGenerator;
        }

        /// <summary>Tries to resolve the schema and returns null if there was a problem.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public string TryResolve(JsonSchema4 schema, string typeNameHint)
        {
            return schema != null ? Resolve(schema, false, typeNameHint) : null;
        }

        /// <summary>Generates the code for all described types (e.g. interfaces, classes, enums, etc).</summary>
        /// <returns>The code.</returns>
        public string GenerateTypes(ExtensionCode extensionCode)
        {
            var processedTypes = new List<string>();
            var types = new Dictionary<string, TypeGeneratorResult>();
            while (_types.Any(t => !processedTypes.Contains(t.Key)))
            {
                foreach (var pair in _types.ToList())
                {
                    processedTypes.Add(pair.Key);
                    var result = pair.Value.GenerateType(pair.Key);
                    types[result.TypeName] = result;
                }
            }

            return string.Join("\n\n", ClassOrderUtilities.Order(types.Values).Select(p =>
            {
                if (extensionCode?.ExtensionClasses.ContainsKey(p.TypeName) == true)
                    return p.Code + "\n\n" + extensionCode.ExtensionClasses[p.TypeName];

                return p.Code;
            }));
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public abstract string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint);

        /// <summary>Adds all schemas to the resolver.</summary>
        /// <param name="definitions">The schema definitions.</param>
        public void AddGenerators(IDictionary<string, JsonSchema4> definitions)
        {
            if (definitions != null)
            {
                foreach (var pair in definitions)
                    AddGenerator(pair.Value.ActualSchema, pair.Key);
            }
        }

        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public virtual string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint)
        {
            schema = schema.ActualSchema;

            if (!_generatedTypeNames.ContainsKey(schema))
            {
                var typeName = schema.GetTypeName(_typeNameGenerator, typeNameHint);

                if (string.IsNullOrEmpty(typeName))
                    typeName = GenerateTypeName(typeNameHint);
                else
                {
                    if (_generatedTypeNames.ContainsValue(typeName))
                        typeName = GenerateTypeName(typeName);
                }

                typeName = typeName
                    .Replace("[", "Of")
                    .Replace("]", string.Empty)
                    .Replace(",", "And")
                    .Replace(" ", string.Empty);

                _generatedTypeNames[schema] = typeName;
            }

            return _generatedTypeNames[schema];
        }

        /// <summary>Generates a unique type name with the given hint.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public string GenerateTypeName(string typeNameHint)
        {
            if (!string.IsNullOrEmpty(typeNameHint))
            {
                typeNameHint = typeNameHint.Split('.').Last();

                if (!_generatedTypeNames.ContainsValue(typeNameHint))
                    return typeNameHint;

                var count = 1;
                do
                {
                    count++;
                } while (_generatedTypeNames.ContainsValue(typeNameHint + count));

                return typeNameHint + count;
            }
            else
                return GenerateTypeName("Anonymous");
        }

        /// <summary>Determines whether the generator for a given type name is registered.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public bool HasTypeGenerator(string typeName)
        {
            return _types.ContainsKey(typeName);
        }

        /// <summary>Creates a type generator.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The generator.</returns>
        protected abstract TGenerator CreateTypeGenerator(JsonSchema4 schema);

        /// <summary>Adds the type generator for a given type name.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="generator">The generator.</param>
        protected void AddOrReplaceTypeGenerator(string typeName, TGenerator generator)
        {
            _types[typeName] = generator;
        }

        /// <summary>Adds a generator for the given schema if necessary.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name of the created generator.</returns>
        protected virtual string AddGenerator(JsonSchema4 schema, string typeNameHint)
        {
            var typeName = GetOrGenerateTypeName(schema, typeNameHint);
            if (!HasTypeGenerator(typeName))
            {
                AddGenerators(schema.Definitions);

                var generator = CreateTypeGenerator(schema);
                AddOrReplaceTypeGenerator(typeName, generator);
            }
            return typeName;
        }

        /// <summary>Resolves the type of the dictionary value of the given schema (must be a dictionary schema).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="fallbackType">The fallback type (e.g. 'object').</param>
        /// <param name="nullHandling">The null handling.</param>
        /// <returns>The type.</returns>
        protected string ResolveDictionaryValueType(JsonSchema4 schema, string fallbackType, NullHandling nullHandling)
        {
            if (schema.AdditionalPropertiesSchema != null)
                return Resolve(schema.AdditionalPropertiesSchema, schema.AdditionalPropertiesSchema.ActualSchema.IsNullable(nullHandling), null);

            if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any())
            {
                var valueTypes = schema.PatternProperties
                    .Select(p => Resolve(p.Value, p.Value.IsNullable(nullHandling), null))
                    .Distinct()
                    .ToList();

                if (valueTypes.Count == 1)
                    return valueTypes.First();
            }

            return fallbackType;
        }
    }
}