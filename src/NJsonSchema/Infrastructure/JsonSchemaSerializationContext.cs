//-----------------------------------------------------------------------
// <copyright file="DynamicApis.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------


using System;

namespace NJsonSchema.Infrastructure
{
    /// <summary>The JSON Schema serialization context holding information about the current serialization.</summary>
    public class JsonSchemaSerializationContext
    {
        [ThreadStatic]
        private static SchemaType _currentSchemaType;

        /// <summary>Gets or sets the current schema type.</summary>
        public static SchemaType CurrentSchemaType
        {
            get => _currentSchemaType;
            set => _currentSchemaType = value;
        }
    }
}
