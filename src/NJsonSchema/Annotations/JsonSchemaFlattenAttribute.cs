//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDateAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to merge all inherited properties into this class/schema.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonSchemaFlattenAttribute : Attribute
    {
    }
}