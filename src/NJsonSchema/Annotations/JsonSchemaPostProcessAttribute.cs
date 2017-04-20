//-----------------------------------------------------------------------
// <copyright file="JsonSchemaAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>0
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------
namespace NJsonSchema.Annotations
{
    using System;

    /// <summary>
    /// Allows to post process schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class JsonSchemaPostProcessAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSchemaPostProcessAttribute"/> class. 
        /// </summary>
        /// <param name="type">Type to get post-process method from.</param>
        /// <param name="methodName">Name of post-process method.</param>
        public JsonSchemaPostProcessAttribute(Type type, string methodName)
        {
            Type = type;
            MethodName = methodName;
        }

        /// <summary>Name of post-process method.</summary>
        public string MethodName { get; }

        /// <summary>Type to get post-process method from.</summary>
        public Type Type { get; }
    }
}