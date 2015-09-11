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
        public static string ConvertToLowerStart(string name)
        {
            return name[0].ToString(CultureInfo.InvariantCulture).ToLower() + name.Substring(1);
        }

        /// <summary>Converts the first letter to upper case.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The converted name. </returns>
        public static string ConvertToUpperStart(string name)
        {
            return name[0].ToString(CultureInfo.InvariantCulture).ToUpper() + name.Substring(1);
        }
    }
}