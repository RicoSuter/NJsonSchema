//-----------------------------------------------------------------------
// <copyright file="ISchemaProcessor.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Threading.Tasks;

namespace NJsonSchema.Generation
{
    /// <summary>The schema processor interface.</summary>
    public interface ISchemaProcessor
    {
        /// <summary>Processes the specified JSON Schema.</summary>
        /// <param name="context">The schema context.</param>
        Task ProcessAsync(SchemaProcessorContext context);
    }
}