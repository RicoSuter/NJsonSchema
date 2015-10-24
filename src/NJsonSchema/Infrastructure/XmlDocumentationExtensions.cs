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
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading XML comments from reflected members.</summary>
    public static class XmlDocumentationExtensions
    {
        private static readonly object _lock = new object();

        private static readonly Dictionary<string, XDocument> _cache =
            new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);

        private static readonly Type _xPathExtensionsType = Type.GetType(
            "System.Xml.XPath.Extensions, " +
            "System.Xml.Linq, Version=4.0.0.0, " +
            "Culture=neutral, PublicKeyToken=b77a5c561934e089");

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this Type type)
        {
            return type.GetTypeInfo().GetXmlDocumentation();
        }

        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlDocumentation(this MemberInfo member)
        {
            if (_xPathExtensionsType == null)
                return string.Empty;

            lock (_lock)
            {
                var assemblyName = member.Module.Assembly.GetName();
                if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
                    return string.Empty;

                return GetXmlDocumentation(member, GetXmlDocumentationPath(member.Module.Assembly));
            }
        }

        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static string GetXmlDocumentation(this ParameterInfo parameter)
        {
            if (_xPathExtensionsType == null)
                return string.Empty;

            lock (_lock)
            {
                var assemblyName = parameter.Member.Module.Assembly.GetName();
                if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
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
                lock (_lock)
                {
                    if (pathToXmlFile == null || _xPathExtensionsType == null)
                        return string.Empty;

                    var assemblyName = member.Module.Assembly.GetName();
                    if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
                        return string.Empty;

                    if (!DynamicFileExists(pathToXmlFile))
                    {
                        _cache[assemblyName.FullName] = null;
                        return string.Empty;
                    }

                    if (!_cache.ContainsKey(assemblyName.FullName))
                        _cache[assemblyName.FullName] = XDocument.Load(pathToXmlFile);

                    return GetXmlDocumentation(member, _cache[assemblyName.FullName]);
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
                lock (_lock)
                {
                    if (pathToXmlFile == null || _xPathExtensionsType == null)
                        return string.Empty;

                    var assemblyName = parameter.Member.Module.Assembly.GetName();
                    if (_cache.ContainsKey(assemblyName.FullName) && _cache[assemblyName.FullName] == null)
                        return string.Empty;

                    if (!DynamicFileExists(pathToXmlFile))
                    {
                        _cache[assemblyName.FullName] = null;
                        return string.Empty;
                    }

                    if (!_cache.ContainsKey(assemblyName.FullName))
                        _cache[assemblyName.FullName] = XDocument.Load(pathToXmlFile);

                    return GetXmlDocumentation(parameter, _cache[assemblyName.FullName]);
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
            return DynamicXPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/summary)", name)).ToString().Trim();
        }

        private static string GetXmlDocumentation(this ParameterInfo parameter, XDocument xml)
        {
            var name = GetMemberElementName(parameter.Member);
            if (parameter.IsRetval || string.IsNullOrEmpty(parameter.Name))
                return DynamicXPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/returns)", name)).ToString().Trim();
            else
                return DynamicXPathEvaluate(xml, string.Format("string(/doc/members/member[@name='{0}']/param[@name='{1}'])", name, parameter.Name)).ToString().Trim();
        }

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

                    var paramTypesList = string.Join(",", ((MethodBase)member).GetParameters().Select(x => x.ParameterType.FullName).ToArray());
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
                    throw new ArgumentException("Unknown member type", "member");
            }
            return string.Format("{0}:{1}", prefixCode, memberName);
        }

        private static string GetXmlDocumentationPath(dynamic assembly)
        {
            var assemblyName = assembly.GetName();
            var path = DynamicPathCombine(DynamicPathGetDirectoryName(assembly.Location), assemblyName.Name + ".xml");
            if (DynamicFileExists(path))
                return path;

            dynamic currentDomain = Type.GetType("System.AppDomain").GetRuntimeProperty("CurrentDomain").GetValue(null);
            return DynamicPathCombine(currentDomain.BaseDirectory, assemblyName.Name + ".xml");
        }

        private static bool DynamicFileExists(string filePath)
        {
            var type = Type.GetType("System.IO.File", true);
            return (bool)type.GetRuntimeMethod("Exists", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        private static string DynamicPathCombine(string path1, string path2)
        {
            var type = Type.GetType("System.IO.Path", true);
            return (string)type.GetRuntimeMethod("Combine", new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { path1, path2 });
        }

        private static string DynamicPathGetDirectoryName(string filePath)
        {
            var type = Type.GetType("System.IO.Path", true);
            return (string)type.GetRuntimeMethod("GetDirectoryName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        private static object DynamicXPathEvaluate(XDocument document, string path)
        {
            return (string)_xPathExtensionsType.GetRuntimeMethod("XPathEvaluate", new[] { typeof(XDocument), typeof(string) }).Invoke(null, new object[] { document, path });
        }
    }
}