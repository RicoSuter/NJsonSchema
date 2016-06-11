//-----------------------------------------------------------------------
// <copyright file="PropertyNameHandling.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema
{
    /// <summary>Defines the property name handling.</summary>
    public enum PropertyNameHandling
    {
        /// <summary>Generates property name using reflection.</summary>
        Default,

        /// <summary>Generates lower camel cased property name using Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver.</summary>
        CamelCase,
    }
}