//-----------------------------------------------------------------------
// <copyright file="JsonSchemaReferenceUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.References;

namespace NJsonSchema
{
    /// <summary>Provides utilities to resolve and set JSON schema references.</summary>
    public static class JsonSchemaReferenceUtilities
    {
        /// <summary>Updates all <see cref="IJsonReferenceBase.Reference"/> properties from the 
        /// available <see cref="IJsonReferenceBase.Reference"/> properties.</summary>
        /// <param name="referenceResolver">The JSON document resolver.</param>
        /// <param name="rootObject">The root object.</param>
        public static async Task UpdateSchemaReferencesAsync(object rootObject, JsonReferenceResolver referenceResolver)
        {
            var updater = new JsonReferenceUpdater(rootObject, referenceResolver);
            await updater.VisitAsync(rootObject).ConfigureAwait(false);
        }

        /// <summary>Converts JSON references ($ref) to property references.</summary>
        /// <param name="data">The data.</param>
        /// <returns>The data.</returns>
        public static string ConvertJsonReferences(string data)
        {
            return data.Replace("$ref", JsonPathUtilities.ReferenceReplaceString);
        }

        /// <summary>Converts property references to JSON references ($ref).</summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static string ConvertPropertyReferences(string data)
        {
            return data.Replace(JsonPathUtilities.ReferenceReplaceString, "$ref");
        }

        /// <summary>Updates the <see cref="IJsonReferenceBase.Reference" /> properties
        /// from the available <see cref="IJsonReferenceBase.Reference" /> properties with inlining external references.</summary>
        /// <param name="rootObject">The root object.</param>
        public static void UpdateSchemaReferencePaths(object rootObject)
        {
            UpdateSchemaReferencePaths(rootObject, false);
        }

        /// <summary>Updates the <see cref="IJsonReferenceBase.Reference" /> properties
        /// from the available <see cref="IJsonReferenceBase.Reference" /> properties.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="removeExternalReferences">Specifies whether to remove external references (otherwise they are inlined).</param>
        public static void UpdateSchemaReferencePaths(object rootObject, bool removeExternalReferences)
        {
            var schemaReferences = new Dictionary<IJsonReference, IJsonReference>();

            var updater = new JsonReferencePathUpdater(rootObject, schemaReferences, removeExternalReferences);
            updater.VisitAsync(rootObject).GetAwaiter().GetResult();

            var searchedSchemas = schemaReferences.Select(p => p.Value).Distinct();
            var result = JsonPathUtilities.GetJsonPaths(rootObject, searchedSchemas);

            foreach (var p in schemaReferences)
                p.Key.ReferencePath = result[p.Value];
        }

        private class JsonReferenceUpdater : JsonSchemaVisitor
        {
            private readonly object _rootObject;
            private readonly JsonReferenceResolver _referenceResolver;
            private bool _replaceRefsRound;

            public JsonReferenceUpdater(object rootObject, JsonReferenceResolver referenceResolver)
            {
                _rootObject = rootObject;
                _referenceResolver = referenceResolver;
            }

            public override async Task VisitAsync(object obj)
            {
                _replaceRefsRound = true;
                await base.VisitAsync(obj);
                _replaceRefsRound = false;
                await base.VisitAsync(obj);
            }

            protected override async Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string typeNameHint)
            {
                if (reference.ReferencePath != null && reference.Reference == null)
                {
                    if (_replaceRefsRound)
                    {
                        if (path.EndsWith("/definitions/" + typeNameHint))
                        {
                            // inline $refs in "definitions"
                            return await _referenceResolver
                                .ResolveReferenceWithoutAppendAsync(_rootObject, reference.ReferencePath)
                                .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // load $refs and add them to "definitions"
                        reference.Reference = await _referenceResolver
                            .ResolveReferenceAsync(_rootObject, reference.ReferencePath)
                            .ConfigureAwait(false);
                    }
                }

                return reference;
            }
        }

        private class JsonReferencePathUpdater : JsonSchemaVisitor
        {
            private readonly object _rootObject;
            private readonly Dictionary<IJsonReference, IJsonReference> _schemaReferences;
            private readonly bool _removeExternalReferences;

            public JsonReferencePathUpdater(object rootObject, Dictionary<IJsonReference, IJsonReference> schemaReferences, bool removeExternalReferences)
            {
                _rootObject = rootObject;
                _schemaReferences = schemaReferences;
                _removeExternalReferences = removeExternalReferences;
            }

#pragma warning disable 1998
            protected override async Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string typeNameHint)
#pragma warning restore 1998
            {
                if (reference.Reference != null)
                {
                    if (!_removeExternalReferences || reference.Reference.DocumentPath == null)
                        _schemaReferences[reference] = reference.Reference.ActualObject;
                    else
                    {
                        var externalReference = reference.Reference;
                        var externalReferenceRoot = externalReference.FindParentDocument();
                        reference.ReferencePath = externalReference.DocumentPath + JsonPathUtilities.GetJsonPath(externalReferenceRoot, externalReference).TrimEnd('#');
                    }
                }
                else if (_removeExternalReferences && _rootObject != reference && reference.DocumentPath != null)
                {
                    throw new NotSupportedException("removeExternalReferences not supported");
                    //return new JsonSchema4 { ReferencePath = reference.DocumentPath };
                }

                return reference;
            }
        }
    }
}