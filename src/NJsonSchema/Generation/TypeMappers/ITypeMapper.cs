//-----------------------------------------------------------------------
// <copyright file="ITypeMapper.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>Maps .NET type to a generated JSON Schema.</summary>
    public interface ITypeMapper
    {
        /// <summary>Gets the mapped type.</summary>
        Type MappedType { get; }

        /// <summary>Gets a value indicating whether to use a JSON Schema reference for the type.</summary>
        bool UseReference { get; }

        /// <summary>Gets the schema for the mapped type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="context">The context.</param>
        void GenerateSchema(JsonSchema schema, TypeMapperContext context);
    }
}