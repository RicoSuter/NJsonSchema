//-----------------------------------------------------------------------
// <copyright file="ISchemaProcessor.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using NJsonSchema.Converters;
using System;
using System.Threading.Tasks;

namespace NJsonSchema.Generation.SchemaProcessors
{
    public class DiscriminatorSchemaProcessor : ISchemaProcessor
    {
        public DiscriminatorSchemaProcessor(Type baseType)
            : this(baseType, JsonInheritanceConverter.DefaultDiscriminatorName)
        {
        }

        public DiscriminatorSchemaProcessor(Type baseType, string discriminator)
        {
            BaseType = baseType;
            Discriminator = discriminator;
        }

        public Type BaseType { get; }

        public string Discriminator { get; }

        public async Task ProcessAsync(SchemaProcessorContext context)
        {
            if (context.Type == BaseType)
            {
                var schema = context.Schema;
                schema.Discriminator = Discriminator;
                schema.Properties[Discriminator] = new JsonProperty
                {
                    Type = JsonObjectType.String,
                    IsRequired = true
                };
            }
        }
    }
}