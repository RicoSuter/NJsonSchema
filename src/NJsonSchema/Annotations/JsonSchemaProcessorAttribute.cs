//-----------------------------------------------------------------------
// <copyright file="JsonSchemaProcessorAttribute.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using NJsonSchema.Generation;

namespace NJsonSchema.Annotations
{
    /// <summary>Registers an schema processor for the given class.</summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class JsonSchemaProcessorAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaProcessorAttribute"/> class.</summary>
        /// <param name="type">The schema processor type (must implement <see cref="ISchemaProcessor"/>).</param>
        /// <param name="parameters">The parameters.</param>
        public JsonSchemaProcessorAttribute(Type type, params object[] parameters)
        {
            Type = type;
            Parameters = parameters;
        }

        /// <summary>Gets or sets the type of the operation processor (must implement IOperationProcessor).</summary>
        public Type Type { get; set; }

        /// <summary>Gets or sets the type of the constructor parameters.</summary>
        public object[] Parameters { get; set; }
    }
}