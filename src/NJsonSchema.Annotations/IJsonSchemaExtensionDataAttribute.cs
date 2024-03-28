//-----------------------------------------------------------------------
// <copyright file="JsonSchemaAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace NJsonSchema.Annotations;

/// <summary>Interface to add an extension data property to a class or property, implementation needs to inherit from System.Attribute.</summary>
#pragma warning disable CA1711 // Rename type name IJsonSchemaExtensionDataAttribute so that it does not end in 'Attribute'
public interface IJsonSchemaExtensionDataAttribute
#pragma warning restore CA1711
{
    /// <summary>Gets the extension data.</summary>
    IReadOnlyDictionary<string, object> ExtensionData { get; }
}