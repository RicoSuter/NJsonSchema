//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        private readonly TypeResolverBase _resolver;
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="GeneratorBase"/> class.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        protected GeneratorBase(object rootObject, TypeResolverBase typeResolver, CodeGeneratorSettingsBase settings)
        {
            RootObject = rootObject;
            _resolver = typeResolver;
            _settings = settings;
        }

        /// <summary>Gets the root object.</summary>
        protected object RootObject { get; }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile(string typeNameHint)
        {
            var schema = (JsonSchema)RootObject;
            return GenerateFile(schema, typeNameHint);
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile()
        {
            var schema = (JsonSchema)RootObject;
            return GenerateFile(schema, schema.Title != null && Regex.IsMatch(schema.Title, "^[a-zA-Z0-9_]*$") ? schema.Title : null);
        }

        /// <summary>Generates the type from the schema and all types from the resolver.</summary>
        /// <param name="schema">The schema</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public IEnumerable<CodeArtifact> GenerateTypes(JsonSchema schema, string typeNameHint)
        {
            _resolver.Resolve(schema, false, typeNameHint); // register root type
            return GenerateTypes();
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile(JsonSchema schema, string typeNameHint)
        {
            var artifacts = GenerateTypes(schema, typeNameHint);
            return GenerateFile(artifacts);
        }

        /// <summary>Generates all types from the resolver.</summary>
        /// <returns>The code.</returns>
        public virtual IEnumerable<CodeArtifact> GenerateTypes()
        {
            // gathers all items that have not yet been processed
            static List<KeyValuePair<JsonSchema, string>> GetItemsRequiringGeneration(
                Dictionary<JsonSchema, string> types,
                HashSet<string> processedTypes)
            {
                var items = new List<KeyValuePair<JsonSchema, string>>();
                foreach (var pair in types)
                {
                    if (!processedTypes.Contains(pair.Value))
                    {
                        items.Add(pair);
                    }
                }

                return items;
            }

            var processedTypes = new HashSet<string>();
            var missing = GetItemsRequiringGeneration(_resolver._generatedTypeNames, processedTypes);

            var types = new Dictionary<string, CodeArtifact>(missing.Count);
            while (missing.Count > 0)
            {
                // generate all, but only include non-ignored in final result
                foreach (var pair in missing)
                {
                    processedTypes.Add(pair.Value);
                    var result = GenerateType(pair.Key, pair.Value);
                    if (!_settings.ExcludedTypeNames.Contains(result.TypeName))
                    {
                        types[result.TypeName] = result;
                    }
                }

                // another pass if needed
                missing = GetItemsRequiringGeneration(_resolver._generatedTypeNames, processedTypes);
            }

            return types.Values;
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        protected abstract string GenerateFile(IEnumerable<CodeArtifact> artifacts);

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        protected abstract CodeArtifact GenerateType(JsonSchema schema, string typeNameHint);
    }
}