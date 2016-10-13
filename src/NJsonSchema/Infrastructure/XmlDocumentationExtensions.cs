//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    /// <remarks>This class currently works only on the desktop .NET framework.</remarks>
    public static class XmlDocumentationExtensions
    {
        private static readonly object Lock = new object();

        private static readonly Dictionary<string, XDocument> Cache =
            new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);

#if !LEGACY

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this Type type)
        {
            return type.GetTypeInfo().GetXmlDocumentation();
        }

#endif

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this MemberInfo member)
        {
            if (DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false)
                return string.Empty;

            lock (Lock)
            {
                var assemblyName = member.Module.Assembly.GetName();
                if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                    return string.Empty;

                return GetXmlDocumentation(member, GetXmlDocumentationPath(member.Module.Assembly));
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static string GetXmlDocumentation(this ParameterInfo parameter)
        {
            if (DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false)
                return string.Empty;

            lock (Lock)
            {
                var assemblyName = parameter.Member.Module.Assembly.GetName();
                if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                    return string.Empty;

                return GetXmlDocumentation(parameter, GetXmlDocumentationPath(parameter.Member.Module.Assembly));
            }
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this Type type, string pathToXmlFile)
        {
            return type.GetTypeInfo().GetXmlDocumentation(pathToXmlFile);
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this MemberInfo member, string pathToXmlFile)
        {
            try
            {
                if (pathToXmlFile == null || DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false)
                    return string.Empty;

                lock (Lock)
                {
                    var assemblyName = member.Module.Assembly.GetName();
                    if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                        return string.Empty;

                    if (!DynamicApis.FileExists(pathToXmlFile))
                    {
                        Cache[assemblyName.FullName] = null;
                        return string.Empty;
                    }

                    if (!Cache.ContainsKey(assemblyName.FullName))
                        Cache[assemblyName.FullName] = XDocument.Load(pathToXmlFile);

                    return GetXmlDocumentation(member, Cache[assemblyName.FullName]);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <param name="pathToXmlFile">The path to the XML documentation file.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static string GetXmlDocumentation(this ParameterInfo parameter, string pathToXmlFile)
        {
            try
            {
                if (pathToXmlFile == null || DynamicApis.SupportsXPathApis == false || DynamicApis.SupportsFileApis == false)
                    return string.Empty;

                lock (Lock)
                {
                    var assemblyName = parameter.Member.Module.Assembly.GetName();
                    if (Cache.ContainsKey(assemblyName.FullName) && Cache[assemblyName.FullName] == null)
                        return string.Empty;

                    if (!DynamicApis.FileExists(pathToXmlFile))
                    {
                        Cache[assemblyName.FullName] = null;
                        return string.Empty;
                    }

                    if (!Cache.ContainsKey(assemblyName.FullName))
                        Cache[assemblyName.FullName] = XDocument.Load(pathToXmlFile);

                    return GetXmlDocumentation(parameter, Cache[assemblyName.FullName]);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetXmlDocumentation(this MemberInfo member, XDocument xml)
        {
            var name = GetMemberElementName(member);
            var documentation = DynamicApis.XPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/summary)", name)).ToString().Trim();
            return RemoveLineBreakWhiteSpaces(documentation);
        }

        private static string GetXmlDocumentation(this ParameterInfo parameter, XDocument xml)
        {
            string documentation;

            var name = GetMemberElementName(parameter.Member);
            if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
                documentation = DynamicApis.XPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/returns)", name)).ToString().Trim();
            else
                documentation = DynamicApis.XPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/param[@name='{1}'])", name, parameter.Name)).ToString().Trim();

            return RemoveLineBreakWhiteSpaces(documentation);
        }

        private static string RemoveLineBreakWhiteSpaces(string documentation)
        {
            return Regex.Replace(documentation.Replace("\r", string.Empty), "\\n\\s+", "\n");
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

        private static string GetXmlDocumentationPath(dynamic assembly)
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

                var path = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(assembly.Location), assemblyName.Name + ".xml");
                if (DynamicApis.FileExists(path))
                    return path;

                if (((object)assembly).GetType().GetRuntimeProperty("CodeBase") != null)
                {
                    path = DynamicApis.PathCombine(DynamicApis.PathGetDirectoryName(assembly.CodeBase.Replace("file:///", string.Empty)), assemblyName.Name + ".xml")
                        .Replace("file:\\", string.Empty);
                    if (DynamicApis.FileExists(path))
                        return path;
                }

                dynamic currentDomain = Type.GetType("System.AppDomain").GetRuntimeProperty("CurrentDomain").GetValue(null);
                path = DynamicApis.PathCombine(currentDomain.BaseDirectory, assemblyName.Name + ".xml");
                if (DynamicApis.FileExists(path))
                    return path;

                return DynamicApis.PathCombine(currentDomain.BaseDirectory, "bin\\" + assemblyName.Name + ".xml");
            }
            catch
            {
                return null;
            }
        }
    }
}
