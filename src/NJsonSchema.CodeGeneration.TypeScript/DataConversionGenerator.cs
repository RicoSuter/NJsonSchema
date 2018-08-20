//-----------------------------------------------------------------------
// <copyright file="DataConversionGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Generates data conversion code.</summary>
    public class DataConversionGenerator
    {
        /// <summary>Generates the code to convert a data object to the target class instances.</summary>
        /// <returns>The generated code.</returns>
        public static string RenderConvertToJavaScriptCode(DataConversionParameters parameters)
        {
            var model = CreateModel(parameters);
            var template = parameters.Settings.TemplateFactory.CreateTemplate("TypeScript", "ConvertToJavaScript", model);
            return template.Render();
        }

        /// <summary>Generates the code to convert a data object to the target class instances.</summary>
        /// <returns>The generated code.</returns>
        public static string RenderConvertToClassCode(DataConversionParameters parameters)
        {
            var model = CreateModel(parameters);
            var template = parameters.Settings.TemplateFactory.CreateTemplate("TypeScript", "ConvertToClass", model);
            return template.Render();
        }

        private static object CreateModel(DataConversionParameters parameters)
        {
            var type = parameters.Resolver.Resolve(parameters.Schema, parameters.IsPropertyNullable, parameters.TypeNameHint);
            var valueGenerator = parameters.Settings.ValueGenerator;

            var dictionaryValueType = parameters.Resolver.TryResolve(parameters.Schema.AdditionalPropertiesSchema, parameters.TypeNameHint) ?? "any";
            var dictionaryValueDefaultValue = parameters.Schema.AdditionalPropertiesSchema != null
                ? valueGenerator.GetDefaultValue(parameters.Schema.AdditionalPropertiesSchema,
                    parameters.Schema.AdditionalPropertiesSchema.IsNullable(parameters.Settings.SchemaType), dictionaryValueType, parameters.TypeNameHint,
                    parameters.Settings.GenerateDefaultValues, parameters.Resolver)
                : null;

            return new
            {
                NullValue = parameters.NullValue.ToString().ToLowerInvariant(),

                Variable = parameters.Variable,
                Value = parameters.Value,

                HasDefaultValue = valueGenerator.GetDefaultValue(parameters.Schema,
                    parameters.IsPropertyNullable, type, parameters.TypeNameHint, parameters.Settings.GenerateDefaultValues, parameters.Resolver) != null,
                DefaultValue = valueGenerator.GetDefaultValue(parameters.Schema,
                    parameters.IsPropertyNullable, type, parameters.TypeNameHint, parameters.Settings.GenerateDefaultValues, parameters.Resolver),

                Type = type,

                IsNewableObject = IsNewableObject(parameters.Schema),
                IsDate = IsDate(parameters.Schema.Format, parameters.Settings.DateTimeType),
                IsDateTime = IsDateTime(parameters.Schema.Format, parameters.Settings.DateTimeType),

                IsDictionary = parameters.Schema.IsDictionary,
                DictionaryValueType = dictionaryValueType,
                DictionaryValueDefaultValue = dictionaryValueDefaultValue,
                HasDictionaryValueDefaultValue = dictionaryValueDefaultValue != null,

                IsDictionaryValueNewableObject = parameters.Schema.AdditionalPropertiesSchema != null && IsNewableObject(parameters.Schema.AdditionalPropertiesSchema),
                IsDictionaryValueDate = IsDate(parameters.Schema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),
                IsDictionaryValueDateTime = IsDateTime(parameters.Schema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),
                IsDictionaryValueNewableArray = parameters.Schema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true &&
                    IsNewableObject(parameters.Schema.AdditionalPropertiesSchema.Item),
                DictionaryValueArrayItemType = parameters.Schema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true ?
                    parameters.Resolver.TryResolve(parameters.Schema.AdditionalPropertiesSchema.Item, "Anonymous") ?? "any" : "any",

                IsArray = parameters.Schema.IsArray,
                ArrayItemType = parameters.Resolver.TryResolve(parameters.Schema.Item, parameters.TypeNameHint) ?? "any",
                IsArrayItemNewableObject = parameters.Schema.Item != null && IsNewableObject(parameters.Schema.Item),
                IsArrayItemDate = IsDate(parameters.Schema.Item?.Format, parameters.Settings.DateTimeType),
                IsArrayItemDateTime = IsDateTime(parameters.Schema.Item?.Format, parameters.Settings.DateTimeType),

                //StringToDateCode is used for date and date-time formats
                UseJsDate = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date,
                StringToDateCode = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date ? "new Date" :
                        (parameters.Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                        parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS) &&
                        parameters.Schema.Format == JsonFormatStrings.TimeSpan ? "moment.duration" :
                    parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS ? "moment.parseZone" : "moment",
                DateTimeToStringCode =
                        (parameters.Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                        parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS) &&
                        parameters.Schema.Format == JsonFormatStrings.TimeSpan ? "format('d.hh:mm:ss.SS', { trim: false })" :
                    parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS ? "toISOString(true)" : "toISOString()",

                HandleReferences = parameters.Settings.HandleReferences
            };
        }

        private static bool IsDateTime(string format, TypeScriptDateTimeType type)
        {
            // TODO: Make this more generic (see TypeScriptTypeResolver.ResolveString)
            if (type == TypeScriptDateTimeType.Date)
            {
                if (format == JsonFormatStrings.DateTime)
                    return true;

                if (format == JsonFormatStrings.Time)
                    return false;

                if (format == JsonFormatStrings.TimeSpan)
                    return false;
            }
            else if (type == TypeScriptDateTimeType.MomentJS ||
                     type == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (format == JsonFormatStrings.DateTime)
                    return true;

                if (format == JsonFormatStrings.Time)
                    return true;

                if (format == JsonFormatStrings.TimeSpan)
                    return true;
            }
            return false;
        }


        private static bool IsDate(string format, TypeScriptDateTimeType type)
        {
            // TODO: Make this more generic (see TypeScriptTypeResolver.ResolveString)
            if (type == TypeScriptDateTimeType.Date)
            {
                if (format == JsonFormatStrings.Date)
                    return true;
            }
            else if (type == TypeScriptDateTimeType.MomentJS ||
                     type == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (format == JsonFormatStrings.Date)
                    return true;
            }
            return false;
        }

        private static bool IsNewableObject(JsonSchema4 schema)
        {
            schema = schema.ActualSchema;
            return (schema.Type.HasFlag(JsonObjectType.Object) || schema.Type == JsonObjectType.None)
                && !schema.IsAnyType && !schema.IsDictionary && !schema.IsEnumeration;
        }
    }
}
