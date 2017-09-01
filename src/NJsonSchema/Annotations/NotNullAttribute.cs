//-----------------------------------------------------------------------
// <copyright file="NotNullAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Indicates that the value of the marked element could never be <c>null</c>.</summary>
    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.ReturnValue |
        AttributeTargets.Delegate |
        AttributeTargets.Field |
        AttributeTargets.Event)]
    public class NotNullAttribute : Attribute
    {
    }
}