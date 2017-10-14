//-----------------------------------------------------------------------
// <copyright file="CodeArtifactCollection.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration
{
    /// <summary></summary>
    public class CodeArtifactCollection
    {
        /// <summary>Initializes a new instance of the <see cref="TypeResolverBase{TGenerator}" /> class.</summary>
        /// <param name="artifacts">The artifacts.</param>
        public CodeArtifactCollection(IEnumerable<CodeArtifact> artifacts)
        {
            Artifacts = OrderByBaseDependency(artifacts);
        }
       
        /// <summary>Gets the artifacts.</summary>
        public IEnumerable<CodeArtifact> Artifacts { get; }
     
        /// <summary>Concatenates the results.</summary>
        /// <returns>The result.</returns>
        public string Concatenate()
        {
            return string.Join("\n\n", Artifacts.Select(p => p.Code));
        }

        /// <summary>Reorders the results so that base classes are always before child classes.</summary>
        /// <param name="results">The results.</param>
        /// <returns>The reordered results.</returns>
        public static IEnumerable<CodeArtifact> OrderByBaseDependency(IEnumerable<CodeArtifact> results)
        {
            var newResults = new List<CodeArtifact>(results);
            foreach (var result in newResults.ToArray())
            {
                if (!string.IsNullOrEmpty(result.BaseTypeName))
                {
                    var index = newResults.IndexOf(result);
                    var baseResult = result;
                    do
                    {
                        baseResult = newResults.SingleOrDefault(r => r.TypeName == baseResult.BaseTypeName);
                        if (baseResult != null)
                        {
                            var baseIndex = newResults.IndexOf(baseResult);
                            if (baseIndex > index)
                            {
                                newResults.RemoveAt(baseIndex);
                                newResults.Insert(index, baseResult);
                            }
                        }
                    } while (baseResult != null);
                }
            }
            return newResults;
        }
    }
}