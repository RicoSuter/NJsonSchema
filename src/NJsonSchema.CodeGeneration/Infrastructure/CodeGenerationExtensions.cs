//-----------------------------------------------------------------------
// <copyright file="CodeGenerationExtensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Reflection;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>Extension methods for code generation.</summary>
    public static class CodeGenerationExtensions
    {
        /// <summary>Gets a method info.</summary>
        /// <param name="type">The type.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The method info.</returns>
        public static MethodInfo GetMethod(this Type type, string method, params Type[] parameters)
        {
            return type.GetRuntimeMethod(method, parameters);
        }
    }
}