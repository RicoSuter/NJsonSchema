//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
        private static readonly Type DecompressionMethodsType;
        private static readonly Type HttpClientHandlerType;
        private static readonly Type HttpClientType;

        static DynamicApis()
        {
            XPathExtensionsType = TryLoadType(
                "System.Xml.XPath.Extensions, System.Xml.XPath.XDocument",
                "System.Xml.XPath.Extensions, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            DecompressionMethodsType = TryLoadType(
                "System.Net.DecompressionMethods, System.Net.Primitives",
                "System.Net.DecompressionMethods, System.Net.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            HttpClientHandlerType = TryLoadType(
                "System.Net.Http.HttpClientHandler, System.Net.Http",
                "System.Net.Http.HttpClientHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            HttpClientType = TryLoadType(
                "System.Net.Http.HttpClient, System.Net.Http",
                "System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            FileType = TryLoadType("System.IO.File, System.IO.FileSystem", "System.IO.File");
            DirectoryType = TryLoadType("System.IO.Directory, System.IO.FileSystem", "System.IO.Directory");
            PathType = TryLoadType("System.IO.Path, System.IO.FileSystem", "System.IO.Path");
        }

        /// <summary>Gets a value indicating whether file APIs are available.</summary>
        public static bool SupportsFileApis => FileType != null;

        /// <summary>Gets a value indicating whether path APIs are available.</summary>
        public static bool SupportsPathApis => PathType != null;

        /// <summary>Gets a value indicating whether path APIs are available.</summary>
        public static bool SupportsDirectoryApis => DirectoryType != null;

        /// <summary>Gets a value indicating whether XPath APIs are available.</summary>
        public static bool SupportsXPathApis => XPathExtensionsType != null;

        /// <summary>Gets a value indicating whether WebClient APIs are available.</summary>
        public static bool SupportsHttpClientApis => HttpClientType != null;

        /// <summary>Request the given URL via HTTP.</summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The content.</returns>
        /// <exception cref="NotSupportedException">The HttpClient.GetAsync API is not available on this platform.</exception>
        public static async Task<string> HttpGetAsync(string url, CancellationToken cancellationToken)
        {
            if (!SupportsHttpClientApis)
            {
                throw new NotSupportedException("The System.Net.Http.HttpClient API is not available on this platform.");
            }

            using (dynamic handler = (IDisposable)Activator.CreateInstance(HttpClientHandlerType))
            using (dynamic client = (IDisposable)Activator.CreateInstance(HttpClientType, new[] { handler }))
            {
                handler.UseDefaultCredentials = true;

                // enable all decompression methods
                var calculatedAllValue = GenerateAllDecompressionMethodsEnumValue();
                var allDecompressionMethodsValue = Enum.ToObject(DecompressionMethodsType, calculatedAllValue);
                handler.AutomaticDecompression = (dynamic)allDecompressionMethodsValue;

                var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        // see https://learn.microsoft.com/en-us/dotnet/api/system.net.decompressionmethods?view=net-7.0
        private static int GenerateAllDecompressionMethodsEnumValue()
        {
            // calculate the set of all possible values (the set will depend on which version of the enum was loaded)
            // NOTE: we can't use All or -1 since those weren't in the 4.0 version of the enum, but we
            // still want to enable additional decompression methods like Brotli (and potentially
            // additional ones in the future) if the loaded httpclient supports it.
            // while the existing values would allow doing a Sum, we still bitwise or to be defensive about
            // potential additions in the future of values like "GZipOrDeflate"
            var calculatedAllValue = Enum.GetValues(DecompressionMethodsType)
                .Cast<int>()
                .Where(val => val > 0) // filter to only positive so we're not including All or None
                .Aggregate(0, (accumulated, newValue) => accumulated | newValue);

            return calculatedAllValue;
        }

        /// <summary>Gets the current working directory.</summary>
        /// <returns>The directory path.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static string DirectoryGetCurrentDirectory()
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            return (string)DirectoryType.GetRuntimeMethod("GetCurrentDirectory", new Type[] { }).Invoke(null, new object[] { });
        }

        /// <summary>Gets the current working directory.</summary>
        /// <returns>The directory path.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static string[] DirectoryGetDirectories(string directory)
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            return (string[])DirectoryType.GetRuntimeMethod("GetDirectories", new[] { typeof(string) }).Invoke(null, new object[] { directory });
        }

        /// <summary>Gets the files of the given directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The file paths.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static string[] DirectoryGetFiles(string directory, string filter)
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            return (string[])DirectoryType.GetRuntimeMethod("GetFiles",
                new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { directory, filter });
        }

        /// <summary>Retrieves the parent directory of the specified path, including both absolute and relative paths..</summary>
        /// <returns>The directory path.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static string DirectoryGetParent(string path)
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            return DirectoryType.GetRuntimeMethod("GetParent", new[] { typeof(string) }).Invoke(null, new object[] { path }).TryGetPropertyValue<string>("FullName");
        }

        /// <summary>Creates a directory.</summary>
        /// <param name="directory">The directory.</param>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static void DirectoryCreateDirectory(string directory)
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            DirectoryType.GetRuntimeMethod("CreateDirectory",
                new[] { typeof(string) }).Invoke(null, new object[] { directory });
        }

        /// <summary>Checks whether a directory exists.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>true or false</returns>
        /// <exception cref="NotSupportedException">The System.IO.Directory API is not available on this platform.</exception>
        public static bool DirectoryExists(string filePath)
        {
            if (!SupportsDirectoryApis)
            {
                throw new NotSupportedException("The System.IO.Directory API is not available on this platform.");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            return (bool)DirectoryType.GetRuntimeMethod("Exists",
                new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        /// <summary>Checks whether a file exists.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>true or false</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static bool FileExists(string filePath)
        {
            if (!SupportsFileApis)
            {
                throw new NotSupportedException("The System.IO.File API is not available on this platform.");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            return (bool)FileType.GetRuntimeMethod("Exists",
                new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        /// <summary>Reads all content of a file (UTF8 with or without BOM).</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The file content.</returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static string FileReadAllText(string filePath)
        {
            if (!SupportsFileApis)
            {
                throw new NotSupportedException("The System.IO.File API is not available on this platform.");
            }

            return (string)FileType.GetRuntimeMethod("ReadAllText",
                new[] { typeof(string), typeof(Encoding) }).Invoke(null, new object[] { filePath, Encoding.UTF8 });
        }

        /// <summary>Writes text to a file (UTF8 without BOM).</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">The System.IO.File API is not available on this platform.</exception>
        public static void FileWriteAllText(string filePath, string text)
        {
            if (!SupportsFileApis)
            {
                throw new NotSupportedException("The System.IO.File API is not available on this platform.");
            }

            // Default of encoding is StreamWriter.UTF8NoBOM
            FileType.GetRuntimeMethod("WriteAllText",
                new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { filePath, text });
        }

        /// <summary>Combines two paths.</summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns>The combined path.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Path API is not available on this platform.</exception>
        public static string PathCombine(string path1, string path2)
        {
            if (!SupportsPathApis)
            {
                throw new NotSupportedException("The System.IO.Path API is not available on this platform.");
            }

            return (string)PathType.GetRuntimeMethod("Combine", new[] { typeof(string), typeof(string) }).Invoke(null, new object[] { path1, path2 });
        }

        /// <summary>Gets the file name from a given path.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The directory name.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Path API is not available on this platform.</exception>
        public static string PathGetFileName(string filePath)
        {
            if (!SupportsPathApis)
            {
                throw new NotSupportedException("The System.IO.Path API is not available on this platform.");
            }

            return (string)PathType.GetRuntimeMethod("GetFileName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        /// <summary>Gets the full path from a given path</summary>
        /// <param name="path">The path</param>
        /// <returns>The full path</returns>
        /// <exception cref="NotSupportedException">The System.IO.Path API is not available on this platform.</exception>
        public static string GetFullPath(string path)
        {
            if (!SupportsPathApis)
            {
                throw new NotSupportedException("The System.IO.Path API is not available on this platform.");
            }

            return (string)PathType.GetRuntimeMethod("GetFullPath", new[] { typeof(string) }).Invoke(null, new object[] { path });
        }

        /// <summary>Gets the directory path of a file path.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The directory name.</returns>
        /// <exception cref="NotSupportedException">The System.IO.Path API is not available on this platform.</exception>
        public static string PathGetDirectoryName(string filePath)
        {
            if (!SupportsPathApis)
            {
                throw new NotSupportedException("The System.IO.Path API is not available on this platform.");
            }

            return (string)PathType.GetRuntimeMethod("GetDirectoryName", new[] { typeof(string) }).Invoke(null, new object[] { filePath });
        }

        /// <summary>Evaluates the XPath for a given XML document.</summary>
        /// <param name="document">The document.</param>
        /// <param name="path">The path.</param>
        /// <returns>The value.</returns>
        /// <exception cref="NotSupportedException">The System.Xml.XPath.Extensions API is not available on this platform.</exception>
        public static object XPathEvaluate(XDocument document, string path)
        {
            if (!SupportsXPathApis)
            {
                throw new NotSupportedException("The System.Xml.XPath.Extensions API is not available on this platform.");
            }

            return XPathExtensionsType.GetRuntimeMethod("XPathEvaluate", new[] { typeof(XDocument), typeof(string) }).Invoke(null, new object[] { document, path });
        }

        /// <summary>
        /// Handle cases of specs in subdirectories having external references to specs also in subdirectories
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="jsonPath"></param>
        /// <returns></returns>
        public static string HandleSubdirectoryRelativeReferences(string fullPath, string jsonPath)
        {
            try
            {
                if (!DynamicApis.DirectoryExists(DynamicApis.PathGetDirectoryName(fullPath)))
                {
                    string fileName = DynamicApis.PathGetFileName(fullPath);
                    string directoryName = DynamicApis.PathGetDirectoryName(fullPath);
                    string folderName = directoryName.Replace("\\", "/").Split('/').Last();
                    if (!string.IsNullOrWhiteSpace(DynamicApis.DirectoryGetParent(directoryName)))
                    {
                        foreach (string subDir in DynamicApis.DirectoryGetDirectories(DynamicApis.DirectoryGetParent(directoryName)))
                        {
                            string expectedDir = DynamicApis.PathCombine(subDir, folderName);
                            string expectedFile = DynamicApis.PathCombine(expectedDir, fileName);
                            if (DynamicApis.DirectoryExists(expectedDir))
                            {
                                fullPath = DynamicApis.PathCombine(expectedDir, fileName);
                                break;
                            }
                        }
                    }
                }

                if (!DynamicApis.FileExists(fullPath))
                {
                    string fileDir = DynamicApis.PathGetDirectoryName(fullPath);
                    if (DynamicApis.DirectoryExists(fileDir))
                    {
                        string fileName = DynamicApis.PathGetFileName(fullPath);
                        string[] pathPieces = fullPath.Replace("\\", "/").Split('/');
                        string subDirPiece = pathPieces[pathPieces.Length - 2];
                        foreach (string subDir in DynamicApis.DirectoryGetDirectories(fileDir))
                        {
                            string expectedFile = DynamicApis.PathCombine(subDir, fileName);
                            if (DynamicApis.FileExists(expectedFile) && DynamicApis.FileReadAllText(expectedFile).Contains(jsonPath.Split('/').Last()))
                            {
                                fullPath = DynamicApis.PathCombine(subDir, fileName);
                                break;
                            }
                        }
                    }
                }

                return fullPath;
            }
            catch
            {
                return fullPath;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Task<T> FromResult<T>(T result)
        {
            return Task.FromResult(result);
        }

        private static Type TryLoadType(params string[] typeNames)
        {
            foreach (var typeName in typeNames)
            {
                try
                {
                    var type = Type.GetType(typeName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }
            return null;
        }
    }
}