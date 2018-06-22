//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    /// <remarks>This class currently works only on the desktop .NET framework.</remarks>
    public static class XmlDocumentationExtensions
    {
        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private static readonly Dictionary<string, XDocument> Cache =
            new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);

#if !LEGACY

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlSummaryAsync(this Type type)
        {
            return GetXmlDocumentationTagAsync(type.GetTypeInfo(), "summary");
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlRemarksAsync(this Type type)
        {
            return GetXmlDocumentationTagAsync(type.GetTypeInfo(), "remarks");
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        [Obsolete("Use GetXmlSummary instead.")]
        public static Task<string> GetXmlDocumentationAsync(this Type type)
        {
            return GetXmlDocumentationTagAsync(type, "summary");
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<string> GetXmlDocumentationTagAsync(this Type type, string tagName)
        {
            return GetXmlDocumentationTagAsync((MemberInfo)type.GetTypeInfo(), tagName);
        }

#endif

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static async Task<string> GetXmlSummaryAsync(this MemberInfo member)
        {
            return await GetXmlDocumentationTagAsync(member, "summary").ConfigureAwait(false);
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static async Task<string> GetXmlRemarksAsync(this MemberInfo member)
        {
            return await GetXmlDocumentationTagAsync(member, "remarks").ConfigureAwait(false);
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static async Task<XElement> GetXmlDocumentationAsync(this MemberInfo member)
        {
            if (DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false)
                return null;

            var assemblyName = member.Module.Assembly.GetName();

#if !LEGACY
            await _lock.WaitAsync();
#else
            _lock.Wait();
#endif
            try
            {
                if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                    return null;
            }
            finally
            {
                _lock.Release();
            }

            var documentationPath = await GetXmlDocumentationPathAsync(member.Module.Assembly).ConfigureAwait(false);
            return await GetXmlDocumentationAsync(member, documentationPath).ConfigureAwait(false);
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static async Task<string> GetXmlDocumentationTagAsync(this MemberInfo member, string tagName)
        {
            if (DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false)
                return string.Empty;

            var assemblyName = member.Module.Assembly.GetName();

#if !LEGACY
            await _lock.WaitAsync();
#else
            _lock.Wait();
#endif
            try
            {
                if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                    return string.Empty;
            }
            finally
            {
                _lock.Release();
            }

            var documentationPath = await GetXmlDocumentationPathAsync(member.Module.Assembly).ConfigureAwait(false);
            var element = await GetXmlDocumentationAsync(member, documentationPath).ConfigureAwait(false);
            return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element?.Element(tagName)));
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static async Task<string> GetXmlDocumentationAsync(this ParameterInfo parameter)
        {
            if (DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false)
                return string.Empty;

            var assemblyName = parameter.Member.Module.Assembly.GetName();

#if !LEGACY
            await _lock.WaitAsync();
#else
            _lock.Wait();
#endif
            try
            {
                if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                    return string.Empty;
            }
            finally
            {
                _lock.Release();
            }

            var documentationPath = await GetXmlDocumentationPathAsync(parameter.Member.Module.Assembly).ConfigureAwait(false);
            var element = await GetXmlDocumentationAsync(parameter, documentationPath).ConfigureAwait(false);
            return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element));
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static Task<XElement> GetXmlDocumentationAsync(this Type type, string pathToXmlFile)
        {
            return ((MemberInfo)type.GetTypeInfo()).GetXmlDocumentationAsync(pathToXmlFile);
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static async Task<XElement> GetXmlDocumentationAsync(this MemberInfo member, string pathToXmlFile)
        {
            try
            {
                if (pathToXmlFile == null || DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false)
                    return null;

                var assemblyName = member.Module.Assembly.GetName();

#if !LEGACY
                await _lock.WaitAsync();
#else
                _lock.Wait();
#endif
                try
                {
                    if (!Cache.ContainsKey(assemblyName.FullName))
                    {
                        if (await DynamicApis.FileExistsAsync(pathToXmlFile).ConfigureAwait(false) == false)
                        {
                            Cache[assemblyName.FullName] = null;
                            return null;
                        }

                        Cache[assemblyName.FullName] = await Task.Factory.StartNew(() => XDocument.Load(pathToXmlFile, LoadOptions.PreserveWhitespace)).ConfigureAwait(false);
                    }
                    else if (Cache[assemblyName.FullName] == null)
                        return null;

                    return GetXmlDocumentation(member, Cache[assemblyName.FullName]);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static async Task<XElement> GetXmlDocumentationAsync(this ParameterInfo parameter, string pathToXmlFile)
        {
            try
            {
                if (pathToXmlFile == null || DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false)
                    return null;

                var assemblyName = parameter.Member.Module.Assembly.GetName();

#if !LEGACY
                await _lock.WaitAsync();
#else
                _lock.Wait();
#endif
                try
                {
                    if (!Cache.ContainsKey(assemblyName.FullName))
                    {
                        if (await DynamicApis.FileExistsAsync(pathToXmlFile).ConfigureAwait(false) == false)
                        {
                            Cache[assemblyName.FullName] = null;
                            return null;
                        }

                        Cache[assemblyName.FullName] = await Task.Factory.StartNew(() => XDocument.Load(pathToXmlFile)).ConfigureAwait(false);
                    }
                    else if (Cache[assemblyName.FullName] == null)
                        return null;

                    return GetXmlDocumentation(parameter, Cache[assemblyName.FullName]);
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch
            {
                return null;
            }
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

        /// <summary>Converts the given XML documentation <see cref="XElement"/> to text.</summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The text</returns>
        public static string GetXmlDocumentationText(this XElement element)
        {
            if (element != null)
            {
                var value = new StringBuilder();
                foreach (var node in element.Nodes())
                {
                    if (node is XElement e)
                    {
                        if (e.Name == "see")
                        {
                            var attribute = e.Attribute("langword");
                            if (attribute != null)
                                value.Append(attribute.Value);
                            else
                            {
                                if (!string.IsNullOrEmpty(e.Value))
                                    value.Append(e.Value);
                                else
                                {
                                    attribute = e.Attribute("cref");
                                    if (attribute != null)
                                        value.Append(attribute.Value.Trim('!', ':').Trim().Split('.').Last());
                                    else
                                    {
                                        attribute = e.Attribute("href");
                                        if (attribute != null)
                                            value.Append(attribute.Value);
                                    }
                                }
                            }
                        }
                        else
                            value.Append(e.Value);
                    }
                    else
                        value.Append(node);
                }

                return value.ToString();
            }

            return null;
        }

        private static XElement GetXmlDocumentation(this MemberInfo member, XDocument xml)
        {
            var name = GetMemberElementName(member);
            var result = (IEnumerable)DynamicApis.XPathEvaluate(xml, $"/doc/members/member[@name='{name}']");
            return result.OfType<XElement>().FirstOrDefault();
        }

        private static XElement GetXmlDocumentation(this ParameterInfo parameter, XDocument xml)
        {
            IEnumerable result;

            var name = GetMemberElementName(parameter.Member);
            if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
                result = (IEnumerable)DynamicApis.XPathEvaluate(xml, $"/doc/members/member[@name='{name}']/returns");
            else
                result = (IEnumerable)DynamicApis.XPathEvaluate(xml, $"/doc/members/member[@name='{name}']/param[@name='{parameter.Name}']");

            return result.OfType<XElement>().FirstOrDefault();
        }

        private static string RemoveLineBreakWhiteSpaces(string documentation)
        {
            if (string.IsNullOrEmpty(documentation))
                return string.Empty;

            documentation = "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

            var whitespace = Regex.Match(documentation, "(\\n[ \\t]*)").Value;
            documentation = documentation.Replace(whitespace, "\n");

            return documentation.Trim('\n');
        }

        /// <exception cref="ArgumentException">Unknown member type.</exception>
        private static string GetMemberElementName(dynamic member)
        {
            char prefixCode;
            string memberName = member is Type ? ((Type)member).FullName : (member.DeclaringType.FullName + "." + member.Name);
            switch ((string)member.MemberType.ToString())
            {
                case "Constructor":
                    memberName = memberName.Replace(".ctor", "#ctor");
                    goto case "Method";

                case "Method":
                    prefixCode = 'M';

                    var paramTypesList = string.Join(",", ((MethodBase)member).GetParameters()
                        .Select(x => Regex
                            .Replace(x.ParameterType.FullName, "(`[0-9]+)|(, .*?PublicKeyToken=[0-9a-z]*)", string.Empty)
                            .Replace("[[", "{")
                            .Replace("]]", "}"))
                        .ToArray());

                    if (!string.IsNullOrEmpty(paramTypesList))
                        memberName += "(" + paramTypesList + ")";
                    break;

                case "Event":
                    prefixCode = 'E';
                    break;

                case "Field":
                    prefixCode = 'F';
                    break;

                case "NestedType":
                    memberName = memberName.Replace('+', '.');
                    goto case "TypeInfo";

                case "TypeInfo":
                    prefixCode = 'T';
                    break;

                case "Property":
                    prefixCode = 'P';
                    break;

                default:
                    throw new ArgumentException("Unknown member type.", "member");
            }
            return string.Format("{0}:{1}", prefixCode, memberName.Replace("+", "."));
        }

        private static async Task<string> GetXmlDocumentationPathAsync(dynamic assembly)
        {
            try
            {
                if (assembly == null)
                    return null;

                if (string.IsNullOrEmpty(assembly.Location))
                    return null;

                var assemblyName = assembly.GetName();
                if (string.IsNullOrEmpty(assemblyName.Name))
                    return null;

                var assemblyDirectory = DynamicApis.PathGetDirectoryName((string)assembly.Location);
                var path = DynamicApis.PathCombine(assemblyDirectory, (string)assemblyName.Name + ".xml");
                if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                    return path;

                if (ReflectionExtensions.HasProperty(assembly, "CodeBase"))
                {
                    path = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(assembly.CodeBase
                        .Replace("file:///", string.Empty)), assemblyName.Name + ".xml")
                        .Replace("file:\\", string.Empty);

                    if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                        return path;
                }

                var currentDomain = Type.GetType("System.AppDomain").GetRuntimeProperty("CurrentDomain").GetValue(null);
                if (currentDomain.HasProperty("BaseDirectory"))
                {
                    var baseDirectory = currentDomain.TryGetPropertyValue("BaseDirectory", "");
                    path = DynamicApis.PathCombine(baseDirectory, assemblyName.Name + ".xml");
                    if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                        return path;

                    return DynamicApis.PathCombine(baseDirectory, "bin\\" + assemblyName.Name + ".xml");
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
