//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Linq;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>Manages the generated types and converts JSON types to TypeScript types. </summary>
    public class TypeScriptTypeResolver : TypeResolverBase
    {
        private readonly object _rootObject;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="settings">The settings.</param>
        public TypeScriptTypeResolver(object rootObject, TypeScriptGeneratorSettings settings)
            : base(settings)
        {
            _rootObject = rootObject;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema. Returns the type name with a 'I' prefix if the feature is supported for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public string ResolveConstructorInterfaceName(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, true);
        }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public override string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            return Resolve(schema, typeNameHint, false);
        }

        /// <summary>Gets a value indicating whether the schema supports constructor conversion.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns>The result.</returns>
        public bool SupportsConstructorConversion(JsonSchema4 schema, object rootObject)
        {
            return schema?.ActualSchema.GetBaseDiscriminator(rootObject) == null;
        }

        private string Resolve(JsonSchema4 schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            schema = schema.ActualSchema;

            if (schema.IsAnyType)
                return "any";

            var type = schema.Type;
            if (type == JsonObjectType.None && schema.IsEnumeration)
            {
                type = schema.Enumeration.All(v => v is int) ?
                    JsonObjectType.Integer :
                    JsonObjectType.String;
            }

            if (type.HasFlag(JsonObjectType.Array))
                return ResolveArrayOrTuple(schema, typeNameHint, addInterfacePrefix);

            if (type.HasFlag(JsonObjectType.Number))
                return "number";

            if (type.HasFlag(JsonObjectType.Integer))
                return ResolveInteger(schema, typeNameHint);

            if (type.HasFlag(JsonObjectType.Boolean))
                return "boolean";

            if (type.HasFlag(JsonObjectType.String))
                return ResolveString(schema, typeNameHint);

            if (type.HasFlag(JsonObjectType.File))
                return "any";

            if (schema.IsDictionary)
            {
                var prefix = addInterfacePrefix &&
                    SupportsConstructorConversion(schema.AdditionalPropertiesSchema, _rootObject) &&
                    schema.AdditionalPropertiesSchema?.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true ? "I" : "";
                var valueType = prefix + ResolveDictionaryValueType(schema, "any", Settings.SchemaType);
                return $"{{ [key: string] : {valueType}; }}";
            }

            return (addInterfacePrefix && SupportsConstructorConversion(schema, _rootObject) ? "I" : "") +
                base.GetOrGenerateTypeName(schema, typeNameHint);
        }

        private string ResolveString(JsonSchema4 schema, string typeNameHint)
        {
            // TODO: Make this more generic (see DataConversionGenerator.IsDate)
            if (Settings.DateTimeType == TypeScriptDateTimeType.Date)
            {
                if (schema.Format == JsonFormatStrings.Date)
                    return "Date";

                if (schema.Format == JsonFormatStrings.DateTime)
                    return "Date";

                if (schema.Format == JsonFormatStrings.Time)
                    return "string";

                if (schema.Format == JsonFormatStrings.TimeSpan)
                    return "string";
            }
            else if (Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                     Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
            {
                if (schema.Format == JsonFormatStrings.Date)
                    return "moment.Moment";

                if (schema.Format == JsonFormatStrings.DateTime)
                    return "moment.Moment";

                if (schema.Format == JsonFormatStrings.Time)
                    return "moment.Moment";

                if (schema.Format == JsonFormatStrings.TimeSpan)
                    return "moment.Moment";
            }

            if (schema.IsEnumeration)
                return GetOrGenerateTypeName(schema, typeNameHint);

            return "string";
        }

        private string ResolveInteger(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.IsEnumeration)
                return GetOrGenerateTypeName(schema, typeNameHint);

            return "number";
        }

        private string ResolveArrayOrTuple(JsonSchema4 schema, string typeNameHint, bool addInterfacePrefix)
        {
            if (schema.Item != null)
            {
                var isObject = schema.Item?.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true;
                var isDictionary = schema.Item?.ActualSchema.IsDictionary == true;
                var prefix = addInterfacePrefix && SupportsConstructorConversion(schema.Item, _rootObject) && isObject && !isDictionary ? "I" : "";
                return string.Format("{0}[]", prefix + Resolve(schema.Item, true, typeNameHint)); // TODO: Make typeNameHint singular if possible
            }

            if (schema.Items != null && schema.Items.Count > 0)
            {
                var tupleTypes = schema.Items
                    .Select(i => Resolve(i.ActualSchema, false, null))
                    .ToArray();

                return string.Format("[" + string.Join(", ", tupleTypes) + "]");
            }

            return "any[]";
        }
    }
}