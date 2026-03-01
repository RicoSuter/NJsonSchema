//-----------------------------------------------------------------------
// <copyright file="CanBeNullAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Annotations;

/// <summary>Indicates that the marked class is ignored during the JSON Schema generation.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class JsonSchemaIgnoreAttribute : Attribute
{
}