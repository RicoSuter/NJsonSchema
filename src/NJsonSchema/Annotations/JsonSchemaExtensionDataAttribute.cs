//-----------------------------------------------------------------------
// <copyright file="JsonSchemaExtensionDataAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Adds an extension data property to a class or property.</summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class JsonSchemaExtensionDataAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaExtensionDataAttribute"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public JsonSchemaExtensionDataAttribute(string property, object value)
        {
            Property = property;
            Value = value;
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaExtensionDataAttribute"/> class.</summary>
        /// <param name="property">The property.</param>
        /// <param name="valueSourceType">The type to get value from.</param>
        /// <param name="valueSourceProperty">The property of the type to get value from.</param>
        public JsonSchemaExtensionDataAttribute(string property, Type valueSourceType, string valueSourceProperty)
        {
            Property = property;
            ValueSourceType = valueSourceType;
            ValueSourceProperty = valueSourceProperty;
            IsValueSourceSpecified = true;
        }

        /// <summary>Gets the property name.</summary>
        public string Property { get; private set; }

        /// <summary>Gets the value.</summary>
        public object Value { get; private set; }

        /// <summary>The property of the type to get value from.</summary>
        public string ValueSourceProperty { get; private set; }

        /// <summary>The type to get value from.</summary>
        public Type ValueSourceType { get; private set; }
        
        /// <summary>Shows that attribute constructed with value source pair.</summary>
        public bool IsValueSourceSpecified { get; private set; }
    }
}