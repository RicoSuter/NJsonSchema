//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
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

        /// <summary>Loads the template from an embedded resource.</summary>
        /// <param name="file">The file name.</param>
        /// <returns>The template. </returns>
        //[Obsolete]
        protected Template LoadTemplate(string file)
        {
            // TODO: Remove LoadTemplate method

            var assembly = GetType().GetTypeInfo().Assembly;
            var prefix = assembly.GetName().Name == "NJsonSchema.CodeGeneration" ? assembly.GetName().Name : assembly.GetName().Name + ".CodeGenerators";
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
    }
}