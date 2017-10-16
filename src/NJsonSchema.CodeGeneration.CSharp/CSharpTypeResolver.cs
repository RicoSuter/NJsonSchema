//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.CSharp.Models;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class CSharpTypeResolver : TypeResolverBase<CSharpGenerator>
    {
        private readonly object _rootObject;

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <param name="rootObject">The root object to search for JSON Schemas.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings, object rootObject)
            : base(settings)
        {
            _rootObject = rootObject;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public override string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            schema = schema.ActualSchema;

            if (schema.IsAnyType)
                return "object";

            var type = schema.Type;
            if (type == JsonObjectType.None && schema.IsEnumeration)
            {
                type = schema.Enumeration.All(v => v is int) ?
                    JsonObjectType.Integer :
                    JsonObjectType.String;
            }

            if (type.HasFlag(JsonObjectType.Array))
                return ResolveArrayOrTuple(schema);

            if (type.HasFlag(JsonObjectType.Number))
                return ResolveNumber(schema, isNullable);

            if (type.HasFlag(JsonObjectType.Integer))
                return ResolveInteger(schema, isNullable, typeNameHint);

            if (type.HasFlag(JsonObjectType.Boolean))
                return ResolveBoolean(isNullable);

            if (type.HasFlag(JsonObjectType.String))
                return ResolveString(schema, isNullable, typeNameHint);

            if (type.HasFlag(JsonObjectType.File))
                return "byte[]";

            if (schema.IsDictionary)
            {
                var valueType = ResolveDictionaryValueType(schema, "object", Settings.SchemaType);
                return string.Format(Settings.DictionaryType + "<string, {0}>", valueType);
            }

            return AddGenerator(schema, typeNameHint);
        }

        /// <inheritdoc />
        public override CodeArtifactCollection GenerateTypes()
        {
            return GenerateTypes(null);
        }

        /// <summary>Generates the code for all described types (e.g. interfaces, classes, enums, etc).</summary>
        /// <returns>The code.</returns>
        public override CodeArtifactCollection GenerateTypes(ExtensionCode extensionCode)
        {
            var collection = base.GenerateTypes(extensionCode);
            var results = new List<CodeArtifact>();

            if (collection.Artifacts.Any(r => r.Code.Contains("JsonInheritanceConverter")))
            {
                results.Add(new CodeArtifact
                {
                    Type = CodeArtifactType.Class,
                    Language = CodeArtifactLanguage.CSharp,

                    TypeName = "JsonInheritanceConverter",
                    Code = Settings.TemplateFactory.CreateTemplate(
                        "CSharp", "JsonInheritanceConverter", new JsonInheritanceConverterTemplateModel(Settings)).Render()
                });
            }

            if (collection.Artifacts.Any(r => r.Code.Contains("DateFormatConverter")))
            {
                results.Add(new CodeArtifact
                {
                    Type = CodeArtifactType.Class,
                    Language = CodeArtifactLanguage.CSharp,

                    TypeName = "DateFormatConverter",
                    Code = Settings.TemplateFactory.CreateTemplate(
                        "CSharp", "DateFormatConverter", new DateFormatConverterTemplateModel(Settings)).Render()
                });
            }

            return new CodeArtifactCollection(collection.Artifacts.Concat(results));
        }

        /// <summary>Adds a generator for the given schema if necessary.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name of the created generator.</returns>
        protected override string AddGenerator(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.IsEnumeration && schema.Type == JsonObjectType.Integer)
            {
                // Regenerate generator because it is be better than the current one (defined enum values)
                var typeName = GetOrGenerateTypeName(schema, typeNameHint);
                var generator = CreateTypeGenerator(schema);
                AddOrReplaceTypeGenerator(typeName, generator);
            }

            return base.AddGenerator(schema, typeNameHint);
        }

        /// <summary>Creates a type generator.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The generator.</returns>
        protected override CSharpGenerator CreateTypeGenerator(JsonSchema4 schema)
        {
            return new CSharpGenerator(schema, Settings, this, _rootObject);
        }

        private string ResolveString(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.Format == JsonFormatStrings.Date)
                return isNullable && Settings.DateType?.ToLowerInvariant() != "string" ? Settings.DateType + "?" : Settings.DateType;

            if (schema.Format == JsonFormatStrings.DateTime)
                return isNullable && Settings.DateTimeType?.ToLowerInvariant() != "string" ? Settings.DateTimeType + "?" : Settings.DateTimeType;

            if (schema.Format == JsonFormatStrings.Time)
                return isNullable && Settings.TimeType?.ToLowerInvariant() != "string" ? Settings.TimeType + "?" : Settings.TimeType;

            if (schema.Format == JsonFormatStrings.TimeSpan)
                return isNullable && Settings.TimeSpanType?.ToLowerInvariant() != "string" ? Settings.TimeSpanType + "?" : Settings.TimeSpanType;

#pragma warning disable 618 // used to resolve type from schemas generated with previous version of the library

            if (schema.Format == JsonFormatStrings.Guid || schema.Format == JsonFormatStrings.Uuid)
                return isNullable ? "System.Guid?" : "System.Guid";

            if (schema.Format == JsonFormatStrings.Base64 || schema.Format == JsonFormatStrings.Byte)
                return "byte[]";

#pragma warning restore 618

            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint) + (isNullable ? "?" : string.Empty);

            return "string";
        }

        private static string ResolveBoolean(bool isNullable)
        {
            return isNullable ? "bool?" : "bool";
        }

        private string ResolveInteger(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint) + (isNullable ? "?" : string.Empty);

            if (schema.Format == JsonFormatStrings.Byte)
                return isNullable ? "byte?" : "byte";

            if (schema.Format == JsonFormatStrings.Long || schema.Format == "long")
                return isNullable ? "long?" : "long";

            return isNullable ? "int?" : "int";
        }

        private static string ResolveNumber(JsonSchema4 schema, bool isNullable)
        {
            if (schema.Format == JsonFormatStrings.Decimal)
                return isNullable ? "decimal?" : "decimal";

            return isNullable ? "double?" : "double";
        }

        private string ResolveArrayOrTuple(JsonSchema4 schema)
        {
            var property = schema;
            if (property.Item != null)
                return string.Format(Settings.ArrayType + "<{0}>", Resolve(property.Item, false, null));

            if (property.Items != null && property.Items.Count > 0)
                return string.Format("System.Tuple<" + string.Join(", ", property.Items.Select(i => Resolve(i.ActualSchema, false, null)) + ">"));

            return Settings.ArrayType + "<object>";
        }
    }
}
