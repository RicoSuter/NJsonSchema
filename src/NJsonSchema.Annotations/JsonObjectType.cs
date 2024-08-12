using System;

namespace NJsonSchema;

/// <summary>
/// Enumeration of the possible object types.
///
/// Keep in sync with NJsonSchema.JsonObjectType
/// </summary>
[Flags]
public enum JsonObjectType
{
    /// <summary>No object type. </summary>
    None = 0,

    /// <summary>An array. </summary>
    Array = 1,

    /// <summary>A boolean value. </summary>
    Boolean = 2,

    /// <summary>An integer value. </summary>
    Integer = 4,

    /// <summary>A null. </summary>
    Null = 8,

    /// <summary>An number value. </summary>
    Number = 16,

    /// <summary>An object. </summary>
    Object = 32,

    /// <summary>A string. </summary>
    String = 64,

    /// <summary>A file (used in Swagger specifications). </summary>
    File = 128,
}