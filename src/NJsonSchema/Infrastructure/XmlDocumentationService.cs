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
    /// <summary>Service to read xml documentation.</summary>
    public class XmlDocumentationService : IXmlDocumentationService
    {
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly Dictionary<string, XDocument> _cache = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);

        private bool IsSupported => DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false || DynamicApis.SupportsPathApis == false;

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public async Task<string> GetXmlSummaryAsync(MemberInfo member)
        {
            return await GetXmlDocumentationAsync(member, "summary").ConfigureAwait(false);
        }

        /// <summary>Returns the contents of the "remarks" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public async Task<string> GetXmlRemarksAsync(MemberInfo member)
        {
            return await GetXmlDocumentationAsync(member, "remarks").ConfigureAwait(false);
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="memberInfo">The member info</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public async Task<string> GetDescriptionAsync(MemberInfo memberInfo, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                return descriptionAttribute.Description;
            }
            else
            {
                dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Description))
                {
                    return displayAttribute.Description;
                }

                if (memberInfo != null)
                {
                    var summary = await GetXmlSummaryAsync(memberInfo).ConfigureAwait(false);
                    if (summary != string.Empty)
                    {
                        return summary;
                    }
                }
            }

            return null;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The description or null if no description is available.</returns>
        public async Task<string> GetDescriptionAsync(ParameterInfo parameter, IEnumerable<Attribute> attributes)
        {
            dynamic descriptionAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                return descriptionAttribute.Description;
            }
            else
            {
                dynamic displayAttribute = attributes.TryGetIfAssignableTo("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Description))
                {
                    return displayAttribute.Description;
                }

                if (parameter != null)
                {
                    var summary = await GetXmlDocumentationAsync(parameter).ConfigureAwait(false);
                    if (summary != string.Empty)
                    {
                        return summary;
                    }
                }
            }

            return null;
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="tagName">Name of the tag.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public async Task<string> GetXmlDocumentationAsync(MemberInfo member, string tagName)
        {
            if (!IsSupported)
            {
                return string.Empty;
            }

            var assemblyName = member.Module.Assembly.GetName();
            using (_lock.Lock())
            {
                if (IsAssemblyIgnored(assemblyName))
                {
                    return string.Empty;
                }

                var element = await GetXmlDocumentationWithoutLockAsync(member).ConfigureAwait(false);
                return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element?.Element(tagName)));
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public async Task<string> GetXmlDocumentationAsync(ParameterInfo parameter)
        {
            if (!IsSupported)
            {
                return string.Empty;
            }

            var assemblyName = parameter.Member.Module.Assembly.GetName();
            using (_lock.Lock())
            {
                if (IsAssemblyIgnored(assemblyName))
                    return string.Empty;

                var element = await GetXmlDocumentationWithoutLockAsync(parameter).ConfigureAwait(false);
                return RemoveLineBreakWhiteSpaces(GetXmlDocumentationText(element));
            }
        }

        /// <summary>Returns the contents of an XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public async Task<XElement> GetXmlDocumentationAsync(MemberInfo member)
        {
            using (_lock.Lock())
            {
                return await GetXmlDocumentationWithoutLockAsync(member).ConfigureAwait(false);
            }
        }

        /// <summary>Clears the cache.</summary>
        /// <returns>The task.</returns>
        public Task ClearCacheAsync()
        {
            using (_lock.Lock())
            {
                _cache.Clear();
                return DynamicApis.FromResult<object>(null);
            }
        }

        /// <summary>Tries to load the xml documentation document for the given assembly.</summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The xml document or null.</returns>
        protected virtual async Task<XDocument> TryGetXmlDocumentAsync(Assembly assembly)
        {
            var pathToXmlFile = await GetXmlDocumentationPathAsync(assembly);
            var assemblyName = assembly.GetName();

            if (!_cache.ContainsKey(assemblyName.FullName))
            {
                if (await DynamicApis.FileExistsAsync(pathToXmlFile).ConfigureAwait(false) == false)
                {
                    _cache[assemblyName.FullName] = null;
                    return null;
                }

                _cache[assemblyName.FullName] = await Task.Factory.StartNew(() => XDocument.Load(pathToXmlFile, LoadOptions.PreserveWhitespace)).ConfigureAwait(false);
            }

            return _cache[assemblyName.FullName];
        }

        /// <summary>Tries to get the xml documentation path for the given assembly.</summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The xml documentation path or null.</returns>
        protected virtual async Task<string> GetXmlDocumentationPathAsync(Assembly assembly)
        {
            string path;
            try
            {
                dynamic dynamicAssembly = assembly;

                if (assembly == null)
                    return null;

                var assemblyName = assembly.GetName();
                if (string.IsNullOrEmpty(assemblyName.Name))
                    return null;

                if (_cache.ContainsKey(assemblyName.FullName))
                    return null;

                if (ReflectionExtensions.HasProperty(assembly, "Location") && !string.IsNullOrEmpty(dynamicAssembly.Location))
                {
                    var assemblyDirectory = DynamicApis.PathGetDirectoryName((string)dynamicAssembly.Location);
                    path = DynamicApis.PathCombine(assemblyDirectory, assemblyName.Name + ".xml");
                    if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                        return path;
                }

                if (ReflectionExtensions.HasProperty(assembly, "CodeBase"))
                {
                    var codeBase = (string)dynamicAssembly.CodeBase;
                    if (!string.IsNullOrEmpty(codeBase))
                    {
                        path = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(codeBase
                            .Replace("file:///", string.Empty)), assemblyName.Name + ".xml")
                            .Replace("file:\\", string.Empty);

                        if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                            return path;
                    }
                }

                var currentDomain = Type.GetType("System.AppDomain")?.GetRuntimeProperty("CurrentDomain").GetValue(null);
                if (currentDomain?.HasProperty("BaseDirectory") == true)
                {
                    var baseDirectory = currentDomain.TryGetPropertyValue("BaseDirectory", "");
                    if (!string.IsNullOrEmpty(baseDirectory))
                    {
                        path = DynamicApis.PathCombine(baseDirectory, assemblyName.Name + ".xml");
                        if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                            return path;

                        return DynamicApis.PathCombine(baseDirectory, "bin\\" + assemblyName.Name + ".xml");
                    }
                }

                var currentDirectory = await DynamicApis.DirectoryGetCurrentDirectoryAsync();
                path = DynamicApis.PathCombine(currentDirectory, assembly.GetName().Name + ".xml");
                if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                {
                    return path;
                }

                path = DynamicApis.PathCombine(currentDirectory, "bin\\" + assembly.GetName().Name + ".xml");
                if (await DynamicApis.FileExistsAsync(path).ConfigureAwait(false))
                {
                    return path;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Converts the given XML documentation <see cref="XElement"/> to text.</summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The text</returns>
        protected virtual string GetXmlDocumentationText(XElement element)
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

        private async Task<XElement> GetXmlDocumentationWithoutLockAsync(MemberInfo member)
        {
            if (!IsSupported)
            {
                return null;
            }

            var assemblyName = member.Module.Assembly.GetName();
            if (IsAssemblyIgnored(assemblyName))
            {
                return null;
            }

            try
            {
                var document = await TryGetXmlDocumentAsync(member.Module.Assembly).ConfigureAwait(false);
                if (document == null)
                    return null;

                var element = GetXmlDocumentation(member, document);
                await ReplaceInheritdocElementsAsync(member, element).ConfigureAwait(false);
                return element;
            }
            catch
            {
                return null;
            }
        }

        private async Task<XElement> GetXmlDocumentationWithoutLockAsync(ParameterInfo parameter)
        {
            try
            {
                if (!IsSupported)
                {
                    return null;
                }

                var document = await TryGetXmlDocumentAsync(parameter.Member.Module.Assembly).ConfigureAwait(false);
                if (document == null)
                {
                    return null;
                }

                return await GetXmlDocumentationAsync(parameter, document).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private bool IsAssemblyIgnored(AssemblyName assemblyName)
        {
            if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
            {
                return true;
            }

            return false;
        }

        private XElement GetXmlDocumentation(MemberInfo member, XDocument document)
        {
            var name = GetMemberElementName(member);
            var result = (IEnumerable)DynamicApis.XPathEvaluate(document, $"/doc/members/member[@name='{name}']");
            return result.OfType<XElement>().FirstOrDefault();
        }

        private async Task<XElement> GetXmlDocumentationAsync(ParameterInfo parameter, XDocument document)
        {
            var name = GetMemberElementName(parameter.Member);
            var result = (IEnumerable)DynamicApis.XPathEvaluate(document, $"/doc/members/member[@name='{name}']");

            var element = result.OfType<XElement>().FirstOrDefault();
            if (element != null)
            {
                await ReplaceInheritdocElementsAsync(parameter.Member, element).ConfigureAwait(false);

                if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
                {
                    result = (IEnumerable)DynamicApis.XPathEvaluate(document, $"/doc/members/member[@name='{name}']/returns");
                }
                else
                {
                    result = (IEnumerable)DynamicApis.XPathEvaluate(document, $"/doc/members/member[@name='{name}']/param[@name='{parameter.Name}']");
                }

                return result.OfType<XElement>().FirstOrDefault();
            }

            return null;
        }

        private async Task ReplaceInheritdocElementsAsync(MemberInfo member, XElement element)
        {
#if !LEGACY
            if (element == null)
            {
                return;
            }

            var children = element.Nodes().ToList();
            foreach (var child in children.OfType<XElement>())
            {
                if (child.Name.LocalName.ToLowerInvariant() == "inheritdoc")
                {
                    var baseType = member.DeclaringType.GetTypeInfo().BaseType;
                    var baseMember = baseType?.GetTypeInfo().DeclaredMembers.SingleOrDefault(m => m.Name == member.Name);
                    if (baseMember != null)
                    {
                        var baseDoc = await GetXmlDocumentationWithoutLockAsync(baseMember).ConfigureAwait(false);
                        if (baseDoc != null)
                        {
                            var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                            child.ReplaceWith(nodes);
                        }
                        else
                        {
                            await ProcessInheritdocInterfaceElementsAsync(member, child).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await ProcessInheritdocInterfaceElementsAsync(member, child).ConfigureAwait(false);
                    }
                }
            }
#endif
        }

#if !LEGACY
        private async Task ProcessInheritdocInterfaceElementsAsync(MemberInfo member, XElement element)
        {
            foreach (var baseInterface in member.DeclaringType.GetTypeInfo().ImplementedInterfaces)
            {
                var baseMember = baseInterface?.GetTypeInfo().DeclaredMembers.SingleOrDefault(m => m.Name == member.Name);
                if (baseMember != null)
                {
                    var baseDoc = await GetXmlDocumentationWithoutLockAsync(baseMember).ConfigureAwait(false);
                    if (baseDoc != null)
                    {
                        var nodes = baseDoc.Nodes().OfType<object>().ToArray();
                        element.ReplaceWith(nodes);
                    }
                }
            }
        }
#endif

        private string RemoveLineBreakWhiteSpaces(string documentation)
        {
            if (string.IsNullOrEmpty(documentation))
            {
                return string.Empty;
            }

            documentation = "\n" + documentation.Replace("\r", string.Empty).Trim('\n');

            var whitespace = Regex.Match(documentation, "(\\n[ \\t]*)").Value;
            documentation = documentation.Replace(whitespace, "\n");

            return documentation.Trim('\n');
        }

        /// <exception cref="ArgumentException">Unknown member type.</exception>
        private string GetMemberElementName(dynamic member)
        {
            char prefixCode;

            var memberName = member is Type memberType && !string.IsNullOrEmpty(memberType.FullName) ?
                memberType.FullName :
                member.DeclaringType.FullName + "." + member.Name;

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

        private class AsyncLock : IDisposable
        {
            private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

            public AsyncLock Lock()
            {
                _semaphoreSlim.Wait();
                return this;
            }

            public void Dispose()
            {
                _semaphoreSlim.Release();
            }
        }
    }
}