//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
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
    internal static class DynamicApis
    {
        private static readonly Type XPathExtensionsType;
        private static readonly Type FileType;
        private static readonly Type PathType;
        private static readonly Type WebClientType;

        static DynamicApis()
        {
            XPathExtensionsType = TryLoadType(
                    "System.Xml.XPath.Extensions, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    "System.Xml.XPath.Extensions, System.Xml.XPath.XDocument");

            FileType = TryLoadType("System.IO.File", "System.IO.File, System.IO.FileSystem");
            PathType = TryLoadType("System.IO.Path", "System.IO.Path, System.IO.FileSystem");

            WebClientType = Type.GetType("System.Net.WebClient, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        }

        public static bool SupportsFileApis => FileType != null && PathType != null;

        public static bool SupportsXPathApis => XPathExtensionsType != null;

        public static bool SupportsWebClientApis => WebClientType != null;

        public static string HttpGet(string url)
        {
            using (dynamic client = (IDisposable)Activator.CreateInstance(WebClientType))
                return client.DownloadString(url);
        }

        public static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return (bool)FileType.GetRuntimeMethod("Exists", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static string FileReadAllText(string filePath)
        {
            return (string)FileType.GetRuntimeMethod("ReadAllText", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static string PathCombine(string path1, string path2)
        {
            return (string)PathType.GetRuntimeMethod("Combine", new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { path1, path2 });
        }

        public static string PathGetDirectoryName(string filePath)
        {
            return (string)PathType.GetRuntimeMethod("GetDirectoryName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        public static object XPathEvaluate(XDocument document, string path)
        {
            return (string)XPathExtensionsType.GetRuntimeMethod("XPathEvaluate", new[] { typeof(XDocument), typeof(string) }).Invoke(null, new object[] { document, path });
        }

        private static Type TryLoadType(params string[] typeNames)
        {
            try
            {
                foreach (var typeName in typeNames)
                {
                    var type = Type.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
    }
}