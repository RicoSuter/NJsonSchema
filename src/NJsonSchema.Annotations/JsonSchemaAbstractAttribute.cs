//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDateAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations;

/// <summary>Annotation to merge all inherited properties into this class/schema.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class JsonSchemaAbstractAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="JsonSchemaAbstractAttribute"/> class.</summary>
    public JsonSchemaAbstractAttribute()
    {
        IsAbstract = true;
    }

    /// <summary>Initializes a new instance of the <see cref="JsonSchemaAbstractAttribute"/> class.</summary>
    /// <param name="isAbstract">The explicit flag to override the global setting (i.e. disable the generation for a type).</param>
    public JsonSchemaAbstractAttribute(bool isAbstract)
    {
        IsAbstract = isAbstract;
    }

    /// <summary>Gets or sets a value indicating whether to set the x-abstract property for given type.</summary>
    public bool IsAbstract { get; }
}