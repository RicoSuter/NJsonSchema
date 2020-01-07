//-----------------------------------------------------------------------
// <copyright file="LiquidKeyValuePair.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>Extension item for a schema.</summary>
    public sealed class ExtensionDataItemModel
    {
        /// <summary>Initializes a new instance of the <see cref="ExtensionDataItemModel"/> class.</summary>
        /// <param name="name">Name of the extension data item.</param>
        /// <param name="value">Value of the extension data item.</param>
        public ExtensionDataItemModel(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>Gets the name of the extension data item.</summary>
        public string Name { get; }

        /// <summary>Gets the value of the extension data item.</summary>
        public object Value { get; }
    }
}