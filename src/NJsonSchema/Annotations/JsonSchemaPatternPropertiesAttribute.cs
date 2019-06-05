//-----------------------------------------------------------------------
// <copyright file="JsonSchemaPatternPropertiesAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to specify the JSON Schema pattern properties.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class JsonSchemaPatternPropertiesAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute" /> class.</summary>
        /// <param name="regularExpression">The pattern property regular expression.</param>
        public JsonSchemaPatternPropertiesAttribute(string regularExpression)
            : this(regularExpression, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute" /> class.</summary>
        /// <param name="regularExpression">The pattern property regular expression.</param>
        /// <param name="type">The pattern properties type.</param>
        public JsonSchemaPatternPropertiesAttribute(string regularExpression, Type type)
        {
            RegularExpression = regularExpression;
            Type = type;
        }

        /// <summary>Gets the pattern properties regular expression.</summary>
        public string RegularExpression { get; }

        /// <summary>Gets the pattern properties type.</summary>
        public Type Type { get; }
    }
}