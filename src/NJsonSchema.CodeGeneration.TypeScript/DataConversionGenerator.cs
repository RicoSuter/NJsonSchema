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

            var typeSchema = parameters.Schema.ActualTypeSchema;
            var dictionaryValueType = parameters.Resolver.TryResolve(typeSchema.AdditionalPropertiesSchema, parameters.TypeNameHint) ?? "any";
            var dictionaryValueDefaultValue = typeSchema.AdditionalPropertiesSchema != null
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

                IsDictionary = typeSchema.IsDictionary,
                DictionaryValueType = dictionaryValueType,
                DictionaryValueDefaultValue = dictionaryValueDefaultValue,
                HasDictionaryValueDefaultValue = dictionaryValueDefaultValue != null,

                IsDictionaryValueNewableObject = typeSchema.AdditionalPropertiesSchema != null && IsNewableObject(typeSchema.AdditionalPropertiesSchema, parameters),
                IsDictionaryValueDate = IsDate(typeSchema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),
                IsDictionaryValueDateTime = IsDateTime(typeSchema.AdditionalPropertiesSchema?.ActualSchema?.Format, parameters.Settings.DateTimeType),
                IsDictionaryValueNewableArray = typeSchema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true &&
                    IsNewableObject(typeSchema.AdditionalPropertiesSchema.Item, parameters),
                DictionaryValueArrayItemType = typeSchema.AdditionalPropertiesSchema?.ActualSchema?.IsArray == true ?
                    parameters.Resolver.TryResolve(typeSchema.AdditionalPropertiesSchema.Item, "Anonymous") ?? "any" : "any",

                IsArray = typeSchema.IsArray,
                ArrayItemType = parameters.Resolver.TryResolve(typeSchema.Item, parameters.TypeNameHint) ?? "any",
                IsArrayItemNewableObject = typeSchema.Item != null && IsNewableObject(typeSchema.Item, parameters),
                IsArrayItemDate = IsDate(typeSchema.Item?.Format, parameters.Settings.DateTimeType),
                IsArrayItemDateTime = IsDateTime(typeSchema.Item?.Format, parameters.Settings.DateTimeType),

                RequiresStrictPropertyInitialization = parameters.Settings.TypeScriptVersion >= 2.7m,

                //StringToDateCode is used for date and date-time formats
                UseJsDate = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date,
                StringToDateCode = parameters.Settings.DateTimeType == TypeScriptDateTimeType.Date ? "new Date" :
                        parameters.Settings.DateTimeType == TypeScriptDateTimeType.DayJS ? "dayjs" :
                        (parameters.Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                        parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS) &&
                        typeSchema.Format == JsonFormatStrings.TimeSpan ? "moment.duration" :
                    parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS ? "moment.parseZone" : "moment",
                DateTimeToStringCode =
                        (parameters.Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                        parameters.Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS) &&
                        typeSchema.Format == JsonFormatStrings.TimeSpan ? "format('d.hh:mm:ss.SS', { trim: false })" :
                        (parameters.Settings.DateTimeType == TypeScriptDateTimeType.DayJS &&
                        typeSchema.Format == JsonFormatStrings.TimeSpan) ? "format('d.hh:mm:ss.SSS')" : 
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
                {
                    return true;
                }

                if (format == JsonFormatStrings.Time)
                {
                    return false;
                }

                if (format == JsonFormatStrings.TimeSpan)
                {
                    return false;
                }
            }
            else if (type == TypeScriptDateTimeType.DayJS || 
                     type == TypeScriptDateTimeType.MomentJS ||
                     type == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (format == JsonFormatStrings.DateTime)
                {
                    return true;
                }

                if (format == JsonFormatStrings.Time)
                {
                    return true;
                }

                if (format == JsonFormatStrings.TimeSpan)
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsDate(string format, TypeScriptDateTimeType type)
        {
            // TODO: Make this more generic (see TypeScriptTypeResolver.ResolveString)
            if (type == TypeScriptDateTimeType.Date)
            {
                if (format == JsonFormatStrings.Date)
                {
                    return true;
                }
            }
            else if (type == TypeScriptDateTimeType.DayJS || 
                     type == TypeScriptDateTimeType.MomentJS ||
                     type == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (format == JsonFormatStrings.Date)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsNewableObject(JsonSchema schema, DataConversionParameters parameters)
        {
            if (schema.ActualTypeSchema.IsEnumeration)
            {
                return false;
            }

            return parameters.Resolver.GeneratesType(schema);
        }
    }
}
