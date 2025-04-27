//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Runtime.Serialization;

using Namotion.Reflection;
using Newtonsoft.Json;

using NJsonSchema.Generation;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading contextual type names and descriptions.</summary>
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<ContextualAccessorInfo, string> _names = [];

        /// <summary>Gets the name of the property for JSON serialization.</summary>
        /// <returns>The name.</returns>
        public static string GetName(this ContextualAccessorInfo accessorInfo)
        {
            return _names.GetOrAdd(accessorInfo, GetNameWithoutCache);
        }

        private static string GetNameWithoutCache(ContextualAccessorInfo accessorInfo)
        {
            var jsonPropertyAttribute = accessorInfo.GetAttribute<JsonPropertyAttribute>(true);
            if (jsonPropertyAttribute != null && !string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
            {
                return jsonPropertyAttribute.PropertyName!;
            }

            var dataMemberAttribute = accessorInfo.GetAttribute<DataMemberAttribute>(true);
            if (dataMemberAttribute != null && !string.IsNullOrEmpty(dataMemberAttribute.Name))
            {
                var dataContractAttribute = accessorInfo
                    .MemberInfo
                    .DeclaringType?
                    .ToCachedType()
                    .GetAttribute<DataContractAttribute>(true);

                if (dataContractAttribute != null)
                {
                    return dataMemberAttribute.Name;
                }
            }

            return accessorInfo.Name;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="type">The member info</param>
        /// <param name="xmlDocsSettings">The XML Docs settings.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static string? GetDescription(this CachedType type, IXmlDocsSettings xmlDocsSettings)
        {
            var attributes = type is ContextualType contextualType ? 
                contextualType.GetContextOrTypeAttributes<Attribute>(true) : 
                type.GetAttributes(true);

            var description = GetDescription(attributes);
            if (description != null)
            {
                return description;
            }

            if (xmlDocsSettings.UseXmlDocumentation)
            {
                var summary = type.GetXmlDocsSummary(xmlDocsSettings.GetXmlDocsOptions());
                if (summary != string.Empty)
                {
                    return summary;
                }
            }

            return null;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="accessorInfo">The accessor info.</param>
        /// <param name="xmlDocsSettings">The XML Docs settings.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static string? GetDescription(this ContextualAccessorInfo accessorInfo, IXmlDocsSettings xmlDocsSettings)
        {
            var description = GetDescription(accessorInfo.GetAttributes(true));
            if (description != null)
            {
                return description;
            }

            if (xmlDocsSettings.UseXmlDocumentation)
            {
                var summary = accessorInfo.MemberInfo.GetXmlDocsSummary(xmlDocsSettings.GetXmlDocsOptions());
                if (summary != string.Empty)
                {
                    return summary;
                }
            }

            return null;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="xmlDocsSettings">The XML Docs settings.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static string? GetDescription(this ContextualParameterInfo parameter, IXmlDocsSettings xmlDocsSettings)
        {
            var description = GetDescription(parameter.GetAttributes(true));
            if (description != null)
            {
                return description;
            }

            if (xmlDocsSettings.UseXmlDocumentation)
            {
                var summary = parameter.GetXmlDocs(xmlDocsSettings.GetXmlDocsOptions());
                if (summary != string.Empty)
                {
                    return summary;
                }
            }

            return null;
        }

        private static string? GetDescription(IEnumerable<Attribute> attributes)
        {
            dynamic? descriptionAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute?.Description))
            {
                return descriptionAttribute!.Description;
            }
            else
            {
                dynamic? displayAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null)
                {
                    // GetDescription returns null if the Description property on the attribute is not specified.
                    var description = displayAttribute.GetDescription();
                    if (description != null)
                    {
                        return description;
                    }
                }
            }

            return null;
        }
    }
}