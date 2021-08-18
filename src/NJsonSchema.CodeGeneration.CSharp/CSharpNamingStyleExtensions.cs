//-----------------------------------------------------------------------
// <copyright file="CSharpGeneratorSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NJsonSchema.CodeGeneration.CSharp
{
    internal static class CSharpNamingStyleExtensions
    {
        private static Dictionary<CSharpNamingStyle, Func<string, string>> _mapping = new Dictionary<CSharpNamingStyle, Func<string, string>>
        {
            { CSharpNamingStyle.FlatCase, ConversionUtilities.ConvertNameToFlatCase },
            { CSharpNamingStyle.UpperFlatCase, ConversionUtilities.ConvertNameToUpperFlatCase },
            { CSharpNamingStyle.CamelCase, ConversionUtilities.ConvertNameToCamelCase },
            { CSharpNamingStyle.PascalCase, ConversionUtilities.ConvertNameToPascalCase },
            { CSharpNamingStyle.SnakeCase, ConversionUtilities.ConvertNameToSnakeCase },
            { CSharpNamingStyle.PascalSnakeCase, ConversionUtilities.ConvertNameToPascalSnakeCase },
        };

        public static string RunConversion(this CSharpNamingStyle namingStyle, string value)
            => _mapping[namingStyle](value);
    }
}
