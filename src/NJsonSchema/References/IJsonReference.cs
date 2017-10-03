//-----------------------------------------------------------------------
// <copyright file="IJsonReference.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.References
{
    /// <summary>A JSON object which may reference other objects with $ref.</summary>
    public interface IJsonReference : IJsonReferenceBase
    {
        /// <summary>Gets the actual referenced object, either this or the reference object.</summary>
        [JsonIgnore]
        IJsonReference ActualObject { get; }

        /// <summary>Gets the parent object of this object. </summary>
        [JsonIgnore]
        object ParentObject { get; }
    }
}