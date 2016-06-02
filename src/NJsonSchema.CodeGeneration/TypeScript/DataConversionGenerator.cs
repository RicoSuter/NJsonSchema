//-----------------------------------------------------------------------
// <copyright file="DataConversionGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using NJsonSchema.CodeGeneration.Models;
using NJsonSchema.CodeGeneration.TypeScript.Templates;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Generates data conversion code.</summary>
    public class DataConversionGenerator
    {
        /// <summary>Generates the code to convert a data object to the target class instances.</summary>
        /// <returns>The generated code.</returns>
        public static string RenderConvertToJavaScriptCode(DataConversionParameters parameters)
        {
            return Render(new ConvertToJavaScriptTemplate(), parameters);
        }

        /// <summary>Generates the code to convert a data object to the target class instances.</summary>
        /// <returns>The generated code.</returns>
        public static string RenderConvertToClassCode(DataConversionParameters parameters)
        {
            return Render(new ConvertToClassTemplate(), parameters);
        }

        private static string Render(ITemplate template, DataConversionParameters parameters)
        {
            template.Initialize(new
            {
                Variable = parameters.Variable,
                Value = parameters.Value,

                HasDefaultValue = PropertyModelBase.GetDefaultValue(parameters.Schema) != null,
                DefaultValue = PropertyModelBase.GetDefaultValue(parameters.Schema),

                Type = parameters.Resolver.Resolve(parameters.Schema, parameters.IsPropertyNullable, parameters.TypeNameHint),

                IsNewableObject = IsNewableObject(parameters.Schema),
                IsDate = parameters.Schema.Format == JsonFormatStrings.DateTime,

                IsDictionary = parameters.Schema.IsDictionary,
                DictionaryValueType = parameters.Resolver.TryResolve(parameters.Schema.AdditionalPropertiesSchema, parameters.TypeNameHint),
                IsDictionaryValueNewableObject = parameters.Schema.AdditionalPropertiesSchema != null && IsNewableObject(parameters.Schema.AdditionalPropertiesSchema),
                IsDictionaryValueDate = parameters.Schema.AdditionalPropertiesSchema?.Format == JsonFormatStrings.DateTime,

                IsArray = parameters.Schema.Type.HasFlag(JsonObjectType.Array),
                ArrayItemType = parameters.Resolver.TryResolve(parameters.Schema.Item, parameters.TypeNameHint),
                IsArrayItemNewableObject = parameters.Schema.Item != null && IsNewableObject(parameters.Schema.Item),
                IsArrayItemDate = parameters.Schema.Item?.Format == JsonFormatStrings.DateTime
            });
            return template.Render();
        }

        private static bool IsNewableObject(JsonSchema4 schema)
        {
            schema = schema.ActualSchema;
            return schema.Type.HasFlag(JsonObjectType.Object) && !schema.IsAnyType && !schema.IsDictionary;
        }
    }
}