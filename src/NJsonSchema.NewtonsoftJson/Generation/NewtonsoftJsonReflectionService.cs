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

namespace NJsonSchema.Generation
{
    public class NewtonsoftJsonReflectionService : ReflectionServiceBase<NewtonsoftJsonSchemaGeneratorSettings>
    {
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
