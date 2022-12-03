using System;
using System.Collections.Generic;

namespace NJsonSchema.Annotations
{
    /// <summary>Interface to add multiple extension data property to a class or property, implementation needs to inherit from System.Attribute.</summary>
    public interface IMultiJsonSchemaExtensionDataAttribute
    {
        /// <summary>Gets the extension data properties dictionary.</summary>
        IDictionary<string, object> SchemaExtensionData { get; }
    }
}