//-----------------------------------------------------------------------
// <copyright file="JsonReferenceExtensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.References
{
    /// <summary>Extensions to work with <see cref="IJsonReference"/>.</summary>
    public static class JsonReferenceExtensions
    {
        /// <summary>Finds the root parent of this schema.</summary>
        /// <returns>The parent schema or this when this is the root.</returns>
        public static object FindParentDocument(this IJsonReference obj)
        {
            if (obj.DocumentPath != null)
                return obj;

            var parent = obj.PossibleRoot;
            if (parent == null)
                return obj;

            while ((parent as IJsonReference)?.PossibleRoot != null)
            {
                parent = ((IJsonReference)parent).PossibleRoot;
                if (parent is IDocumentPathProvider pathProvider && pathProvider.DocumentPath != null)
                    return parent;
            }

            return parent;
        }
    }
}