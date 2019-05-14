//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    /// <remarks>This class currently works only on the desktop .NET framework.</remarks>
    public static class XmlDocumentationExtensions
    {
        private static Dictionary<MemberWithContext, string> _names = new Dictionary<MemberWithContext, string>();

        /// <summary>Gets the name of the property for JSON serialization.</summary>
        /// <returns>The name.</returns>
        public static string GetName(this MemberWithContext member)
        {
            if (!_names.ContainsKey(member))
            {
                lock (_names)
                {
                    if (!_names.ContainsKey(member))
                    {
                        // TODO: Move somewhere else as this is not XML docs related
                        _names[member] = GetNameWithoutCache(member);
                    }
                }
            }
            return _names[member];
        }

        private static string GetNameWithoutCache(MemberWithContext member)
        {
            var jsonPropertyAttribute = member.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && !string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
                return jsonPropertyAttribute.PropertyName;

            var dataMemberAttribute = member.GetContextAttribute<DataMemberAttribute>();
            if (dataMemberAttribute != null && !string.IsNullOrEmpty(dataMemberAttribute.Name))
            {
                var dataContractAttribute = member.MemberInfo.DeclaringType.GetTypeWithoutContext().GetTypeAttribute<DataContractAttribute>();
                if (dataContractAttribute != null)
                {
                    return dataMemberAttribute.Name;
                }
            }

            return member.Name;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="memberInfo">The member info</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static async Task<string> GetDescriptionAsync(this MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description;
            else
            {
                dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Description))
                    return displayAttribute.Description;

                if (memberInfo != null)
                {
                    var summary = await memberInfo.GetXmlSummaryAsync().ConfigureAwait(false);
                    if (summary != string.Empty)
                        return summary;
                }
            }

            return null;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static async Task<string> GetDescriptionAsync(this ParameterInfo parameter, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                return descriptionAttribute.Description;
            else
            {
                dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Description))
                    return displayAttribute.Description;

                if (parameter != null)
                {
                    var summary = await parameter.GetXmlDocumentationAsync().ConfigureAwait(false);
                    if (summary != string.Empty)
                        return summary;
                }
            }

            return null;
        }
    }
}