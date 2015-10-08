//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.IO;
using Antlr4.StringTemplate;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        /// <summary>Gets the language.</summary>
        protected abstract string Language { get; }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public abstract string GenerateFile();

        /// <summary>Generates the type.</summary>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public abstract string GenerateType(string typeNameHint);

        /// <summary>Loads the template from an embedded resource.</summary>
        /// <param name="file">The file name.</param>
        /// <returns>The template. </returns>
        protected Template LoadTemplate(string file)
        {
            var assembly = GetType().Assembly;
            var prefix = assembly.GetName().Name == "NJsonSchema.CodeGeneration" ? assembly.GetName().Name : assembly.GetName().Name + ".ClientGenerators";
            var resourceName = string.Format("{0}.{1}.Templates.{2}.txt", prefix, Language, file);
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    return new Template(text);
                }
            }
        }

        /// <summary>Converts the first letter to lower case.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The converted name. </returns>
        public static string ConvertToLowerStartIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return (name[0].ToString(CultureInfo.InvariantCulture).ToLower() + name.Substring(1)).Replace(" ", "_");
        }

        /// <summary>Converts the first letter to upper case.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The converted name. </returns>
        public static string ConvertToUpperStartIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return (name[0].ToString(CultureInfo.InvariantCulture).ToUpper() + name.Substring(1)).Replace(" ", "_");
        }
    }
}