//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    /// <remarks>This class currently works only on the desktop .NET framework.</remarks>
    public static class XmlDocumentationExtensions
    {
        /// <summary>The default service.</summary>
        public static IXmlDocumentationService Service { get; } = new XmlDocumentationService();

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlSummaryAsync(this MemberInfo member)
        {
            return Service.GetXmlDocumentationAsync(member, "summary");
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlRemarksAsync(this MemberInfo member)
        {
            return Service.GetXmlDocumentationAsync(member, "remarks");
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="memberInfo">The member info</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static Task<string> GetDescriptionAsync(this MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            return Service.GetDescriptionAsync(memberInfo, attributes);
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static Task<string> GetDescriptionAsync(this ParameterInfo parameter, IEnumerable<Attribute> attributes)
        {
            return Service.GetDescriptionAsync(parameter, attributes);
        }

        /// <summary>Clears the cache.</summary>
        /// <returns>The task.</returns>
        public static Task ClearCacheAsync()
        {
            return Service.ClearCacheAsync();
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlDocumentationTagAsync(this MemberInfo member, string tagName)
        {
            return Service.GetXmlDocumentationAsync(member, tagName);
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static Task<string> GetXmlDocumentationAsync(this ParameterInfo parameter)
        {
            return Service.GetXmlDocumentationAsync(parameter);
        }
        
        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<XElement> GetXmlDocumentationAsync(this MemberInfo member)
        {
            return Service.GetXmlDocumentationAsync(member);
        }
    }
}