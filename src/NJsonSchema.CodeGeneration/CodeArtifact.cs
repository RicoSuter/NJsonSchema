//-----------------------------------------------------------------------
// <copyright file="TypeGeneratorResult.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type generator result.</summary>
    public class CodeArtifact
    {
        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="category">The category.</param>
        /// <param name="code">The code.</param>
        public CodeArtifact(string typeName, CodeArtifactType type, CodeArtifactLanguage language, CodeArtifactCategory category, string code)
            : this(typeName, null, type, language, category, code)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="category">The category.</param>
        /// <param name="template">The template to render the code.</param>
        public CodeArtifact(string typeName, CodeArtifactType type, CodeArtifactLanguage language, CodeArtifactCategory category, ITemplate template)
            : this(typeName, null, type, language, category, template.Render())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="baseTypeName">The base type name (e.g. base class).</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="category">The category.</param>
        /// <param name="template">The template to render the code.</param>
        public CodeArtifact(string typeName, string? baseTypeName, CodeArtifactType type, CodeArtifactLanguage language, CodeArtifactCategory category, ITemplate template)
            : this(typeName, baseTypeName, type, language, category, template.Render())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="baseTypeName">The base type name (e.g. base class).</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="category">The category.</param>
        /// <param name="code">The code.</param>
        public CodeArtifact(string typeName, string? baseTypeName, CodeArtifactType type, CodeArtifactLanguage language, CodeArtifactCategory category, string code)
        {
            if (typeName == baseTypeName)
            {
                throw new ArgumentException($"The baseTypeName '{baseTypeName}' cannot equal typeName.", nameof(typeName));
            }

            TypeName = typeName;
            BaseTypeName = baseTypeName;

            Type = type;
            Language = language;
            Category = category;
            Code = code;
        }

        /// <summary>Gets the type name.</summary>
        public string TypeName { get; }

        /// <summary>Gets the name of the base type (i.e. the name of the inherited class).</summary>
        public string? BaseTypeName { get; }

        /// <summary>Gets the artifact type.</summary>
        public CodeArtifactType Type { get; }

        /// <summary>Get the artifact language.</summary>
        public CodeArtifactLanguage Language { get; }

        /// <summary>Gets the category.</summary>
        public CodeArtifactCategory Category { get; }

        /// <summary>Gets the generated code.</summary>
        public string Code { get; }
    }
}