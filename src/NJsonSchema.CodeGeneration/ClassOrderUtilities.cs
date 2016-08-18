//-----------------------------------------------------------------------
// <copyright file="ClassOrderUtilities.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration
{
    internal static class ClassOrderUtilities
    {
        /// <summary>Reorders the results so that base classes are always before child classes.</summary>
        /// <param name="results">The results.</param>
        /// <returns>The reordered results.</returns>
        public static IEnumerable<TypeGeneratorResult> Order(IEnumerable<TypeGeneratorResult> results)
        {
            var newResults = new List<TypeGeneratorResult>(results);
            foreach (var result in newResults.ToArray())
            {
                if (!string.IsNullOrEmpty(result.BaseTypeName))
                {
                    var index = newResults.IndexOf(result);

                    var baseResult = newResults.SingleOrDefault(r => r.TypeName == result.BaseTypeName);
                    if (baseResult != null)
                    {
                        var baseIndex = newResults.IndexOf(baseResult);
                        if (baseIndex > index)
                        {
                            newResults.RemoveAt(baseIndex);
                            newResults.Insert(index, baseResult);
                        }
                    }
                }
            }
            return newResults;
        }
    }
}
