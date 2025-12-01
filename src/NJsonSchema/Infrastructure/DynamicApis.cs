//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides dynamic access to framework APIs.</summary>
    public static class DynamicApis
    {
        private static readonly Type? HttpClientType;
        private static readonly Type? HttpClientHandlerType;
        private static readonly Type? DecompressionMethodsType;

        static DynamicApis()
        {
            HttpClientType = TryLoadType(
                "System.Net.Http.HttpClient, System.Net.Http",
                "System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            HttpClientHandlerType = TryLoadType(
                "System.Net.Http.HttpClientHandler, System.Net.Http",
                "System.Net.Http.HttpClientHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            DecompressionMethodsType = TryLoadType(
                "System.Net.DecompressionMethods, System.Net.Primitives",
                "System.Net.DecompressionMethods, System.Net.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        }

        /// <summary>Gets a value indicating whether WebClient APIs are available.</summary>
        public static bool SupportsHttpClientApis => HttpClientType != null && HttpClientHandlerType != null && DecompressionMethodsType != null;

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

            using dynamic handler = (IDisposable)Activator.CreateInstance(HttpClientHandlerType!)!;
            using dynamic client = (IDisposable)Activator.CreateInstance(HttpClientType!, [handler])!;
            handler.UseDefaultCredentials = true;

            // enable all decompression methods
            var calculatedAllValue = GenerateAllDecompressionMethodsEnumValue();
            var allDecompressionMethodsValue = Enum.ToObject(DecompressionMethodsType!, calculatedAllValue);
            handler.AutomaticDecompression = (dynamic)allDecompressionMethodsValue;

            var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
            var calculatedAllValue = Enum.GetValues(DecompressionMethodsType!)
                .Cast<int>()
                .Where(val => val > 0) // filter to only positive so we're not including All or None
                .Aggregate(0, (accumulated, newValue) => accumulated | newValue);

            return calculatedAllValue;
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
                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                {
                    var fileName = Path.GetFileName(fullPath);
                    var directoryName = Path.GetDirectoryName(fullPath)!;
                    var folderName = directoryName.Replace("\\", "/").Split('/').Last();
                    var parentDirectory = Directory.GetParent(directoryName);
                    if (!string.IsNullOrWhiteSpace(parentDirectory?.FullName))
                    {
                        foreach (string subDir in Directory.GetDirectories(parentDirectory!.FullName))
                        {
                            var expectedDir = Path.Combine(subDir, folderName);
                            var expectedFile = Path.Combine(expectedDir, fileName);
                            if (Directory.Exists(expectedDir))
                            {
                                fullPath = Path.Combine(expectedDir, fileName);
                                break;
                            }
                        }
                    }
                }

                if (!File.Exists(fullPath))
                {
                    var fileDir = Path.GetDirectoryName(fullPath);
                    if (Directory.Exists(fileDir))
                    {
                        var fileName = Path.GetFileName(fullPath);
                        foreach (var subDir in Directory.GetDirectories(fileDir))
                        {
                            var expectedFile = Path.Combine(subDir, fileName);
                            if (File.Exists(expectedFile) && File.ReadAllText(expectedFile).Contains(jsonPath.Split('/').Last()))
                            {
                                fullPath = Path.Combine(subDir, fileName);
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

        private static Type? TryLoadType(params string[] typeNames)
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