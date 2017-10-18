//-----------------------------------------------------------------------
// <copyright file="TypeGeneratorResult.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The type generator result.</summary>
    public class CodeArtifact
    {
        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        public CodeArtifact(string typeName, CodeArtifactType type, CodeArtifactLanguage language)
            : this(typeName, null, type, language, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="baseTypeName">The base type name (e.g. base class).</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        public CodeArtifact(string typeName, string baseTypeName, CodeArtifactType type, CodeArtifactLanguage language)
            : this(typeName, baseTypeName, type, language, null)
        {
            BaseTypeName = baseTypeName;
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="template">The template to render the code.</param>
        public CodeArtifact(string typeName, CodeArtifactType type, CodeArtifactLanguage language, ITemplate template)
            : this(typeName, null, type, language, template)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CodeArtifact"/> class.</summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="baseTypeName">The base type name (e.g. base class).</param>
        /// <param name="type">The artifact type.</param>
        /// <param name="language">The artifact language.</param>
        /// <param name="template">The template to render the code.</param>
        public CodeArtifact(string typeName, string baseTypeName, CodeArtifactType type, CodeArtifactLanguage language, ITemplate template)
        {
            TypeName = typeName;
            BaseTypeName = baseTypeName;

            Type = type;
            Language = language;

            Code = template?.Render();
        }

        /// <summary>Gets the type name.</summary>
        public string TypeName { get; }

        /// <summary>Gets the name of the base type (i.e. the name of the inherited class).</summary>
        public string BaseTypeName { get; }

        /// <summary>Gets the artifact type.</summary>
        public CodeArtifactType Type { get; }

        /// <summary>Get the artifact language.</summary>
        public CodeArtifactLanguage Language { get; }

        /// <summary>Gets or sets the generated code.</summary>
        public string Code { get; set; }
    }
}