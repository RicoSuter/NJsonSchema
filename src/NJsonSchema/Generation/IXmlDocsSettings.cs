//-----------------------------------------------------------------------
// <copyright file="IXmlDocsSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation
{
    /// <summary>The XML Docs related settings.</summary>
    public interface IXmlDocsSettings
    {
        /// <summary>Gets or sets a value indicating whether to read XML Docs (default: true).</summary>
        bool UseXmlDocs { get; }
    }
}