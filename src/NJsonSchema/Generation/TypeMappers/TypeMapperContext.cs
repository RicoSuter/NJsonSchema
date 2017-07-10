//-----------------------------------------------------------------------
// <copyright file="TypeMapperContext.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>The context object for the <see cref="ITypeMapper"/> interface.</summary>
    public class TypeMapperContext
    {
        /// <summary>Initializes a new instance of the <see cref="TypeMapperContext"/> class.</summary>
        public TypeMapperContext(
            Type type, 
            JsonSchemaGenerator jsonSchemaGenerator,
            JsonSchemaResolver jsonSchemaResolver, 
            IEnumerable<Attribute> parentAttributes)
        {
            Type = type;
            JsonSchemaGenerator = jsonSchemaGenerator;
            JsonSchemaResolver = jsonSchemaResolver;
            ParentAttributes = parentAttributes;
        }

        /// <summary>The source type.</summary>
        public Type Type { get;  }

        /// <summary>The <see cref="JsonSchemaGenerator"/>.</summary>
        public JsonSchemaGenerator JsonSchemaGenerator { get;  }

        /// <summary>The <see cref="JsonSchemaResolver"/>.</summary>
        public JsonSchemaResolver JsonSchemaResolver { get;  }

        /// <summary>The parent properties (e.g. the attributes on the property).</summary>
        public IEnumerable<Attribute> ParentAttributes { get;  }
    }
}