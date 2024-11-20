//-----------------------------------------------------------------------
// <copyright file="DataConversionGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
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

            JsonSchema typeSchema = parameters.Schema.ActualTypeSchema;

            var dictionaryValueType = typeSchema.AdditionalPropertiesSchema != null ?
                parameters.Resolver.TryResolve(typeSchema.AdditionalPropertiesSchema, parameters.TypeNameHint) ?? "any" :
                null;

            var dictionaryValueDefaultValue = typeSchema.AdditionalPropertiesSchema != null && dictionaryValueType != null
                ? valueGenerator.GetDefaultValue(typeSchema.AdditionalPropertiesSchema,
                    typeSchema.AdditionalPropertiesSchema.IsNullable(parameters.Settings.SchemaType), dictionaryValueType, parameters.TypeNameHint,
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

                CheckNewableObject = parameters.CheckNewableObject,
                IsNewableObject = IsNewableObject(parameters.Schema, parameters),
                IsDate = IsDate(typeSchema.Format, parameters.Settings.DateTimeType),
                IsDateTime = IsDateTime(typeSchema.Format, parameters.Settings.DateTimeType),

                // Dictionary
                IsDictionary = typeSchema.IsDictionary,
                DictionaryValueType = dictionaryValueType,
                DictionaryValueDefaultValue = dictionaryValueDefaultValue,
                HasDictionaryValueDefaultValue = dictionaryValueDefaultValue != null,

                IsDictionaryValueNewableObject = typeSchema.AdditionalPropertiesSchema != null && IsNewableObject(typeSchema.AdditionalPropertiesSchema, parameters),
                IsDictionaryValueDate = IsDate(typeSchema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),
                IsDictionaryValueDateTime = IsDateTime(typeSchema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),

                IsDictionaryValueNewableArray =
                    typeSchema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true &&
                    typeSchema.AdditionalPropertiesSchema.Item != null &&
                    IsNewableObject(typeSchema.AdditionalPropertiesSchema.Item, parameters),

                DictionaryValueArrayItemType =
                    typeSchema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true ?
                    parameters.Resolver.TryResolve(typeSchema.AdditionalPropertiesSchema.Item, "Anonymous") ?? "any" :
                    "any",

                // Array
                IsArray = typeSchema.IsArray,
                ArrayItemType = parameters.Resolver.TryResolve(typeSchema.Item, parameters.TypeNameHint) ?? "any",
                IsArrayItemNewableObject = typeSchema.Item != null && IsNewableObject(typeSchema.Item, parameters),
                IsArrayItemDate = IsDate(typeSchema.Item?.Format, parameters.Settings.DateTimeType),
                IsArrayItemDateTime = IsDateTime(typeSchema.Item?.Format, parameters.Settings.DateTimeType),

                RequiresStrictPropertyInitialization = parameters.Settings.TypeScriptVersion >= 2.7m,

                // Dates
                //StringToDateCode is used for date and date-time formats
                UseJsDate = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date,
                StringToDateCode = GetStringToDateTime(parameters, typeSchema),
                StringToDateOnlyCode = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date
                                       && parameters.Settings.ConvertDateToLocalTimezone
                    ? "parseDateOnly"
                    : GetStringToDateTime(parameters, typeSchema),
                DateToStringCode = GetDateToString(parameters, typeSchema),
                DateTimeToStringCode = GetDateTimeToString(parameters, typeSchema),

                HandleReferences = parameters.Settings.HandleReferences
            };
        }

        private static string GetStringToDateTime(DataConversionParameters parameters, JsonSchema typeSchema)
        {
            switch (parameters.Settings.DateTimeType)
            {
                case TypeScriptDateTimeType.Date:
                    return "new Date";

                case TypeScriptDateTimeType.MomentJS:
                case TypeScriptDateTimeType.OffsetMomentJS:
                    if (typeSchema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
                    {
                        return "moment.duration";
                    }

                    if (parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
                    {
                        return "moment.parseZone";
                    }

                    return "moment";

                case TypeScriptDateTimeType.String:
                    return "";

                case TypeScriptDateTimeType.Luxon:
                    if (typeSchema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
                    {
                        return "Duration.fromISO";
                    }
                    return "DateTime.fromISO";

                case TypeScriptDateTimeType.DayJS:
                    return "dayjs";

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameters));
            }
        }

        private static string GetDateToString(DataConversionParameters parameters, JsonSchema typeSchema)
        {
            switch (parameters.Settings.DateTimeType)
            {
                case TypeScriptDateTimeType.Date:
                case TypeScriptDateTimeType.String:
                    return "";

                case TypeScriptDateTimeType.MomentJS:
                case TypeScriptDateTimeType.OffsetMomentJS:
                case TypeScriptDateTimeType.DayJS:
                    return "format('YYYY-MM-DD')";

                case TypeScriptDateTimeType.Luxon:
                    return "toFormat('yyyy-MM-dd')";

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameters));
            }
        }

        private static string GetDateTimeToString(DataConversionParameters parameters, JsonSchema typeSchema)
        {
            switch (parameters.Settings.DateTimeType)
            {
                case TypeScriptDateTimeType.Date:
                    return "toISOString()";

                case TypeScriptDateTimeType.MomentJS:
                case TypeScriptDateTimeType.OffsetMomentJS:
                    if (typeSchema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
                    {
                        return "format('d.hh:mm:ss.SS', { trim: false })";
                    }

                    if (parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
                    {
                        return "toISOString(true)";
                    }
                    return "toISOString()";

                case TypeScriptDateTimeType.String:
                    return "";

                case TypeScriptDateTimeType.Luxon:
                    return "toString()";

                case TypeScriptDateTimeType.DayJS:
                    if (typeSchema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
                    {
                        return "format('d.hh:mm:ss.SSS')";
                    }

                    return "toISOString()";

                default:
                    throw new ArgumentOutOfRangeException(nameof(parameters));
            }
        }

        private static bool IsDateTime(string? format, TypeScriptDateTimeType type)
        {
            // TODO: Make this more generic (see TypeScriptTypeResolver.ResolveString)
            if (type == TypeScriptDateTimeType.Date)
            {
                return format == JsonFormatStrings.DateTime;
            }

            if (type is TypeScriptDateTimeType.DayJS or TypeScriptDateTimeType.MomentJS or TypeScriptDateTimeType.OffsetMomentJS)
            {
                return format is JsonFormatStrings.DateTime or JsonFormatStrings.Time or JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan;
            }

            if (type == TypeScriptDateTimeType.Luxon)
            {
                return format is JsonFormatStrings.DateTime or JsonFormatStrings.Time or JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan;
            }
            return false;
        }


        private static bool IsDate(string? format, TypeScriptDateTimeType type)
        {
            // TODO: Make this more generic (see TypeScriptTypeResolver.ResolveString)
            if (type == TypeScriptDateTimeType.Date)
            {
                return format == JsonFormatStrings.Date;
            }

            if (type is TypeScriptDateTimeType.DayJS or TypeScriptDateTimeType.MomentJS or TypeScriptDateTimeType.OffsetMomentJS)
            {
                return format == JsonFormatStrings.Date;
            }

            if (type == TypeScriptDateTimeType.Luxon)
            {
                return format == JsonFormatStrings.Date;
            }
            return false;
        }

        private static bool IsNewableObject(JsonSchema? schema, DataConversionParameters parameters)
        {
            if (schema != null)
            {
                if (schema.ActualTypeSchema.IsEnumeration)
                {
                    return false;
                }

                return parameters.Resolver.GeneratesType(schema);
            }

            return false;
        }
    }
}