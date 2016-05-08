//-----------------------------------------------------------------------
// <copyright file="CSharpClassStyle.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.ComponentModel;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp styles.</summary>
    public enum CSharpClassStyle
    {
        /// <summary>Generates POCOs (Plain Old C# Objects).</summary>
        Poco,
        
        /// <summary>Generates classes implementing the <see cref="INotifyPropertyChanged"/> interface.</summary>
        Inpc
    }
}