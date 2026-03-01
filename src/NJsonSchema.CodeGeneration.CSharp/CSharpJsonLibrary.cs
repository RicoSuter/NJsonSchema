//-----------------------------------------------------------------------
// <copyright file="CSharpClassStyle.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp JSON library to use.</summary>
    public enum CSharpJsonLibrary
    {
        /// <summary>Use Newtonsoft.Json</summary>
        NewtonsoftJson, 

        /// <summary>Use System.Text.Json</summary>
        SystemTextJson
    }
}