//-----------------------------------------------------------------------
// <copyright file="FullDotNetMethods.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    internal static class FullDotNetMethods
    {
        private static readonly Type XPathExtensionsType = Type.GetType(
            "System.Xml.XPath.Extensions, " +
            "System.Xml.Linq, Version=4.0.0.0, " +
            "Culture=neutral, PublicKeyToken=b77a5c561934e089");

        public static bool SupportsFullDotNetMethods
        {
            get { return XPathExtensionsType != null; }
        }

        public static bool FileExists(string filePath)
        {
            var type = Type.GetType("System.IO.File", true);
            return (bool)type.GetRuntimeMethod("Exists", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static string FileReadAllText(string filePath)
        {
            var type = Type.GetType("System.IO.File", true);
            return (string)type.GetRuntimeMethod("ReadAllText", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static string PathCombine(string path1, string path2)
        {
            var type = Type.GetType("System.IO.Path", true);
            return (string)type.GetRuntimeMethod("Combine", new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { path1, path2 });
        }

        public static string PathGetDirectoryName(string filePath)
        {
            var type = Type.GetType("System.IO.Path", true);
            return (string)type.GetRuntimeMethod("GetDirectoryName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static object XPathEvaluate(XDocument document, string path)
        {
            return (string)XPathExtensionsType.GetRuntimeMethod("XPathEvaluate", new[] { typeof(XDocument), typeof(string) }).Invoke(null, new object[] { document, path });
        }
    }
}