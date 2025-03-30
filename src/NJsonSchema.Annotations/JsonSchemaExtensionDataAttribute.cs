﻿//-----------------------------------------------------------------------
// <copyright file="JsonSchemaExtensionDataAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Annotations;

/// <summary>Adds an extension data property to a class or property.</summary>
/// <seealso cref="System.Attribute" />
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue,
    AllowMultiple = true)]
public class JsonSchemaExtensionDataAttribute : Attribute, IJsonSchemaExtensionDataAttribute
{
    /// <summary>Initializes a new instance of the <see cref="JsonSchemaExtensionDataAttribute"/> class.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public JsonSchemaExtensionDataAttribute(string key, object value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>Gets the property name.</summary>
    public string Key { get; }

    /// <summary>Gets the value.</summary>
    public object Value { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> ExtensionData => new Dictionary<string, object>
    {
        { Key, Value }
    };
}