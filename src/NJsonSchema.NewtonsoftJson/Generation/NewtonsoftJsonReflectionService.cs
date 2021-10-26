//-----------------------------------------------------------------------
// <copyright file="DefaultReflectionService.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using Newtonsoft.Json.Converters;
using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Serialization;

namespace NJsonSchema.Generation
{
    public class NewtonsoftJsonReflectionService : ReflectionServiceBase<NewtonsoftJsonSchemaGeneratorSettings>
    {
        protected override JsonTypeDescription GetDescription(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings, 
            Type originalType, bool isNullable, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var contract = settings.ResolveContract(originalType);
            if (contract is JsonStringContract)
            {
                return JsonTypeDescription.Create(contextualType, JsonObjectType.String, isNullable, null);
            }

            return base.GetDescription(contextualType, settings, originalType, isNullable, defaultReferenceTypeNullHandling);
        }

        public override bool IsNullable(ContextualType contextualType, ReferenceTypeNullHandling defaultReferenceTypeNullHandling)
        {
            var jsonPropertyAttribute = contextualType.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && jsonPropertyAttribute.Required == Required.DisallowNull)
            {
                return false;
            }

            return base.IsNullable(contextualType, defaultReferenceTypeNullHandling);
        }

        public override bool IsStringEnum(ContextualType contextualType, NewtonsoftJsonSchemaGeneratorSettings settings)
        {
            if (settings.SerializerSettings.Converters.OfType<StringEnumConverter>().Any())
            {
                return true;
            }

            return base.IsStringEnum(contextualType, settings);
        }
    }
}
