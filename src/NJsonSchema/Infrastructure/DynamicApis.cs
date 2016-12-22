//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides dynamic access to framework APIs.</summary>
    public static class DynamicApis
    {
        private static readonly Type XPathExtensionsType;
        private static readonly Type FileType;
        private static readonly Type DirectoryType;
        private static readonly Type PathType;
        private static readonly Type HttpClientType;

        static DynamicApis()
        {
            XPathExtensionsType = TryLoadType(
                "System.Xml.XPath.Extensions, System.Xml.XPath.XDocument",
                "System.Xml.XPath.Extensions, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            HttpClientType = TryLoadType(
                "System.Net.Http.HttpClient, System.Net.Http",
                "System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            FileType = TryLoadType("System.IO.File, System.IO.FileSystem", "System.IO.File");
            DirectoryType = TryLoadType("System.IO.Directory, System.IO.FileSystem", "System.IO.Directory");
            PathType = TryLoadType("System.IO.Path, System.IO.FileSystem", "System.IO.Path");
        }

        /// <summary>Gets a value indicating whether file APIs are available.</summary>
        public static bool SupportsFileApis => FileType != null && PathType != null;

        /// <summary>Gets a value indicating whether XPath APIs are available.</summary>
        public static bool SupportsXPathApis => XPathExtensionsType != null;

        /// <summary>Gets a value indicating whether WebClient APIs are available.</summary>
        public static bool SupportsHttpClientApis => HttpClientType != null;

        /// <summary>Request the given URL via HTTP.</summary>
        /// <param name="url">The URL.</param>
        /// <returns>The content.</returns>
        public static async Task<string> HttpGetAsync(string url)
        {
            using (dynamic client = (IDisposable) Activator.CreateInstance(HttpClientType))
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>Gets the current working directory.</summary>
        /// <returns>The directory path.</returns>
        public static Task<string> DirectoryGetCurrentDirectoryAsync()
        {
            return Task.Factory.StartNew(() => (string)DirectoryType.GetRuntimeMethod("GetCurrentDirectory", new Type[] { }).Invoke(null, new object[] { }));
        }

        /// <summary>Gets the files of the given directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The file paths.</returns>
        public static Task<string[]> DirectoryGetFilesAsync(string directory, string filter)
        {
            return Task.Factory.StartNew(() => (string[])DirectoryType.GetRuntimeMethod("GetFiles", 
                new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { directory, filter }));
        }

        /// <summary>Creates a directory.</summary>
        /// <param name="directory">The directory.</param>
        public static Task DirectoryCreateDirectoryAsync(string directory)
        {
            return Task.Factory.StartNew(() => DirectoryType.GetRuntimeMethod("CreateDirectory", 
                new[] { typeof(string) }).Invoke(null, new object[] { directory }));
        }

        /// <summary>Checks whether a directory exists.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>true or false</returns>
        public static async Task<bool> DirectoryExistsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return await Task.Factory.StartNew(() => (bool)DirectoryType.GetRuntimeMethod("Exists", 
                new[] { typeof(string) }).Invoke(null, new object[] { filePath }));
        }

        /// <summary>Checks whether a file exists.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>true or false</returns>
        public static async Task<bool> FileExistsAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return await Task.Factory.StartNew(() => (bool)FileType.GetRuntimeMethod("Exists", 
                new[] { typeof(string) }).Invoke(null, new object[] { filePath }));
        }

        /// <summary>Reads all content of a file (UTF8).</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The file content.</returns>
        public static Task<string> FileReadAllTextAsync(string filePath)
        {
            return Task.Factory.StartNew(() =>(string)FileType.GetRuntimeMethod("ReadAllText", 
                new[] { typeof(string), typeof(Encoding) }).Invoke(null, new object[] { filePath, Encoding.UTF8 }));
        }

        /// <summary>Writes text to a file (UTF8).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static Task FileWriteAllTextAsync(string filePath, string text)
        {
            return Task.Factory.StartNew(() => FileType.GetRuntimeMethod("WriteAllText",
                new[] { typeof(string), typeof(string), typeof(Encoding) }).Invoke(null, new object[] { filePath, text, Encoding.UTF8 }));
        }

        /// <summary>Combines two paths.</summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns>The combined path.</returns>
        public static string PathCombine(string path1, string path2)
        {
            return (string)PathType.GetRuntimeMethod("Combine", new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { path1, path2 });
        }

        /// <summary>Gets the directory path of a file path.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The directory name.</returns>
        public static string PathGetDirectoryName(string filePath)
        {
            return (string)PathType.GetRuntimeMethod("GetDirectoryName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        /// <summary>Evaluates the XPath for a given XML document.</summary>
        /// <param name="document">The document.</param>
        /// <param name="path">The path.</param>
        /// <returns>The value.</returns>
        public static string XPathEvaluate(XDocument document, string path)
        {
            return (string)XPathExtensionsType.GetRuntimeMethod("XPathEvaluate", new[] { typeof(XDocument), typeof(string) }).Invoke(null, new object[] { document, path });
        }

        private static Type TryLoadType(params string[] typeNames)
        {
            foreach (var typeName in typeNames)
            {
                try
                {
                    var type = Type.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }
    }
}