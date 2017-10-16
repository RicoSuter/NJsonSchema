//-----------------------------------------------------------------------
// <copyright file="JsonReferenceBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.References
{
    /// <summary>A base class which may reference another object.</summary>
    /// <typeparam name="T">The referenced object type.</typeparam>
    public abstract class JsonReferenceBase<T> : IJsonReferenceBase
        where T : class, IJsonReference
    {
        private T _reference;

        /// <summary>Gets the document path (URI or file path) for resolving relative references.</summary>
        [JsonIgnore]
        public string DocumentPath { get; set; }

        /// <summary>Gets or sets the type reference path ($ref). </summary>
        [JsonProperty(JsonPathUtilities.ReferenceReplaceString, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        string IJsonReferenceBase.ReferencePath { get; set; }

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        public virtual T Reference
        {
            get => _reference;
            set
            {
                if (_reference != value)
                {
                    _reference = value;
                    ((IJsonReferenceBase)this).ReferencePath = null;
                }
            }
        }

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        IJsonReference IJsonReferenceBase.Reference
        {
            get => Reference;
            set => Reference = (T)value;
        }
    }
}