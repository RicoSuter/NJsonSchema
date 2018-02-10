//-----------------------------------------------------------------------
// <copyright file="CanBeNullAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Indicates that the marked class is ignored during the JSON Schema generation.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonSchemaIgnoreAttribute : Attribute
    {
    }
}