//-----------------------------------------------------------------------
// <copyright file="JsonSchemaReferenceUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using NJsonSchema.References;
using NJsonSchema.Visitors;

namespace NJsonSchema
{
    /// <summary>Provides utilities to resolve and set JSON schema references.</summary>
    public static class JsonSchemaReferenceUtilities
    {
        /// <summary>Updates all <see cref="IJsonReferenceBase.Reference"/> properties from the
        /// available <see cref="IJsonReferenceBase.Reference"/> properties.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="referenceResolver">The JSON document resolver.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static async Task UpdateSchemaReferencesAsync(object rootObject, JsonReferenceResolver referenceResolver, CancellationToken cancellationToken = default)
        {
            var updater = new JsonReferenceUpdater(rootObject, referenceResolver);
            await updater.VisitAsync(rootObject, cancellationToken).ConfigureAwait(false);
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
            updater.Visit(rootObject);

            var searchedSchemas = schemaReferences.Select(p => p.Value).Distinct();
            var result = JsonPathUtilities.GetJsonPaths(rootObject, searchedSchemas);

            foreach (var p in schemaReferences)
            {
                p.Key.ReferencePath = result[p.Value];
            }
        }

        private sealed class JsonReferenceUpdater : AsyncJsonReferenceVisitorBase
        {
            private readonly object _rootObject;
            private readonly JsonReferenceResolver _referenceResolver;
            private bool _replaceRefsRound;

            public JsonReferenceUpdater(object rootObject, JsonReferenceResolver referenceResolver)
            {
                _rootObject = rootObject;
                _referenceResolver = referenceResolver;
            }

            public override async Task VisitAsync(object obj, CancellationToken cancellationToken = default)
            {
                _replaceRefsRound = true;
                await base.VisitAsync(obj, cancellationToken).ConfigureAwait(false);
                _replaceRefsRound = false;
                await base.VisitAsync(obj, cancellationToken).ConfigureAwait(false);
            }

            protected override async Task<IJsonReference> VisitJsonReferenceAsync(IJsonReference reference, string path, string? typeNameHint, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reference.ReferencePath != null && reference.Reference == null)
                {
                    if (_replaceRefsRound)
                    {
                        if (path.EndsWith("/definitions/" + typeNameHint, StringComparison.Ordinal) || path.EndsWith("/schemas/" + typeNameHint, StringComparison.Ordinal))
                        {
                            // inline $refs in "definitions"
                            return await _referenceResolver
                                .ResolveReferenceWithoutAppendAsync(_rootObject, reference.ReferencePath, reference.GetType(), cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // load $refs and add them to "definitions"
                        reference.Reference = await _referenceResolver
                            .ResolveReferenceAsync(_rootObject, reference.ReferencePath, reference.GetType(), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                return reference;
            }
        }

        private sealed class JsonReferencePathUpdater : JsonReferenceVisitorBase
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

            protected override IJsonReference VisitJsonReference(IJsonReference reference, string path, string? typeNameHint)
            {
                if (reference.Reference != null)
                {
                    if (!_removeExternalReferences || reference.Reference.DocumentPath == null)
                    {
                        _schemaReferences[reference] = reference.Reference.ActualObject;
                    }
                    else
                    {
                        var externalReference = reference.Reference;
                        var externalReferenceRoot = externalReference.FindParentDocument();
                        if (externalReferenceRoot != null)
                        {
                            var jsonPath = JsonPathUtilities.GetJsonPath(
                                externalReferenceRoot, externalReference)?.TrimEnd('#');

                            reference.ReferencePath = externalReference.DocumentPath + jsonPath;
                        }
                    }
                }
                else if (_removeExternalReferences && _rootObject != reference && reference.DocumentPath != null)
                {
                    throw new NotSupportedException("removeExternalReferences not supported");
                }

                return reference;
            }
        }
    }
}
