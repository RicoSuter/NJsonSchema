//-----------------------------------------------------------------------
// <copyright file="IXmlDocsSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// XML Documentation settings extensions.
    /// </summary>
    public static class XmlDocsSettingsExtensions
    {
        /// <summary>
        /// Converts a settings to options.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The options.</returns>
        public static XmlDocsOptions GetXmlDocsOptions(this IXmlDocsSettings settings)
        {
            return new XmlDocsOptions
            {
                ResolveExternalXmlDocs = settings.ResolveExternalXmlDocumentation,
                FormattingMode = settings.XmlDocumentationFormatting
            };
        }
    }
}