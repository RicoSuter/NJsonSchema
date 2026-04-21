//-----------------------------------------------------------------------
// <copyright file="NotNullAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Annotations;

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