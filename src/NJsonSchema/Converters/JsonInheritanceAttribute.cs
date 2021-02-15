//-----------------------------------------------------------------------
// <copyright file="JsonInheritanceConverter.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Converters
{
    /// <summary>Defines a child class in the inheritance chain.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class JsonInheritanceAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonInheritanceAttribute"/> class.</summary>
        /// <param name="key">The discriminator key.</param>
        /// <param name="type">The child class type.</param>
        public JsonInheritanceAttribute(string key, Type type)
        {
            Key = key;
            Type = type;
        }

        /// <summary>Gets the discriminator key.</summary>
        public string Key { get; }

        /// <summary>Gets the child class type.</summary>
        public Type Type { get; }
    }
}