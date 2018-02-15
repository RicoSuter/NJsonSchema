//-----------------------------------------------------------------------
// <copyright file="IDocumentPathProvider.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace PageUp.NJsonSchema
{
    /// <summary>Provides a property to get a documents path or base URI.</summary>
    public interface IDocumentPathProvider
    {
        /// <summary>Gets the document path (URI or file path).</summary>
        string DocumentPath { get; }
    }
}