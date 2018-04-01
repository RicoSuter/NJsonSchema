//-----------------------------------------------------------------------
// <copyright file="OpenApiDiscriminator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary>Describes a schema discriminator.</summary>
    public class OpenApiDiscriminator
    {
        /// <summary>Gets or sets the discriminator property name.</summary>
        [JsonProperty("propertyName", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string PropertyName { get; set; }

        /// <summary>Gets or sets the discriminator mappings.</summary>
        [JsonProperty("mapping", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public IDictionary<string, JsonSchema4> Mapping { get; } = new Dictionary<string, JsonSchema4>();

        /// <summary>The currently used <see cref="JsonInheritanceConverter"/>.</summary>
        [JsonIgnore]
        public object JsonInheritanceConverter { get; set; }

        /// <summary>Adds a discriminator mapping for the given type and schema based on the used <see cref="JsonInheritanceConverter"/>.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schema">The schema.</param>
        public void AddMapping(Type type, JsonSchema4 schema)
        {
            dynamic converter = JsonInheritanceConverter;

            var getDiscriminatorValueMethod = JsonInheritanceConverter.GetType()
#if LEGACY
                .GetMethod(nameof(Converters.JsonInheritanceConverter.GetDiscriminatorValue), new Type[] { typeof(Type) });
#else
                .GetRuntimeMethod(nameof(Converters.JsonInheritanceConverter.GetDiscriminatorValue), new Type[] { typeof(Type) });
#endif

            if (getDiscriminatorValueMethod != null)
            {
                var discriminatorValue = converter.GetDiscriminatorValue(type);
                Mapping[discriminatorValue] = new JsonSchema4 { Reference = schema.ActualSchema };
            }
            else
                Mapping[type.Name] = new JsonSchema4 { Reference = schema.ActualSchema };
        }
    }
}