//-----------------------------------------------------------------------
// <copyright file="ITemplateFactory.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The interface of a code generator template factory.</summary>
    public interface ITemplateFactory
    {
        /// <summary>Creates a template for the given language, template name and template model.</summary>
        /// <param name="language">The language (i.e. 'CSharp' or 'TypeScript').</param>
        /// <param name="template">The template name.</param>
        /// <param name="model">The template model.</param>
        /// <returns>The template.</returns>
        ITemplate CreateTemplate(string language, string template, object model);
    }
}