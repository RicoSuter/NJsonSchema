//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    /// <summary>The CSharp OneOf template model.</summary>
    public sealed class OneOfTemplateModel : TemplateModelBase
    {
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="OneOfTemplateModel" /> class.</summary>
        /// <param name="arity">The number of different types the OneOf can take</param>
        /// <param name="namedDetails">Properties which pertain to a named OneOf type</param>
        /// <param name="settings">The settings.</param>
        public OneOfTemplateModel(int arity, NamedOneOfDetails namedDetails, CSharpGeneratorSettings settings)
        {
            Arity = arity;
            NamedDetails = namedDetails;
            _settings = settings;
        }

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => _settings.TypeAccessModifier;

        /// <summary>The number of different types the OneOf can take</summary>
        public int Arity { get; }

        /// <summary>The numbers of each case in the OneOf type</summary>
        public IEnumerable<int> CaseNumbers => Enumerable.Range(1, Arity);

        /// <summary>Properties which pertain to a named OneOf type</summary>
        public NamedOneOfDetails NamedDetails { get; }
    }

    /// <summary>Properties which pertain to a named OneOf type</summary>
    public sealed class NamedOneOfDetails
    {
        /// <summary>Initializes a new instance of the <see cref="NamedOneOfDetails" /> class.</summary>
        /// <param name="name">Name of the type.</param>
        /// <param name="hasDescription">The schema.</param>
        /// <param name="description">The settings.</param>
        /// <param name="typeArguments">The names of the type arguments of the OneOf type.</param>
        public NamedOneOfDetails(string name, bool hasDescription, string description, IReadOnlyList<string> typeArguments)
        {
            Name = name;
            HasDescription = hasDescription;
            Description = description;
            TypeArguments = typeArguments;
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription { get; }

        /// <summary>Gets the description.</summary>
        public string Description { get; }

        /// <summary>Gets the names of the type arguments of the OneOf type.</summary>
        public IReadOnlyList<string> TypeArguments { get; }

    }

}