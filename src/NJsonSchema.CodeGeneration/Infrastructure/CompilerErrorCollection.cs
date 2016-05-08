//-----------------------------------------------------------------------
// <copyright file="CompilerErrorCollection.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.CodeDom.Compiler
{
    // Needed for T4 in PCL libraries

    /// <summary>Internal.</summary>
    public class CompilerErrorCollection : List<CompilerError>
    {
        
    }

    /// <summary>Internal.</summary>
    public class CompilerError
    {
        /// <summary>Gets or sets the error text.</summary>
        public string ErrorText { get; set; }

        /// <summary>Gets or sets a value indicating whether this error is a warning.</summary>
        public bool IsWarning { get; set; }
    }
}