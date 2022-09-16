//-----------------------------------------------------------------------
// <copyright file="IXmlDocsSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;

namespace NJsonSchema.Generation
{
    internal static class XmlDocsSettingsExtensions
    {
        internal static XmlDocsOptions GetXmlDocsOptions(this IXmlDocsSettings settings)
        {
            return new XmlDocsOptions
            {
                ResolveExternalXmlDocs = settings.ResolveExternalXmlDocumentation,
                FormattingMode = settings.XmlDocumentationFormatting
            };
        }
    }
}