//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.CSharp.Templates;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class CSharpTypeResolver : TypeResolverBase<CSharpGenerator>
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings)
            : base(settings.TypeNameGenerator)
        {
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; private set; }

        /// <summary>Adds all schemas to the resolver.</summary>
        /// <param name="schemas">The schemas (typeNameHint-schema pairs).</param>
        public override void AddSchemas(IDictionary<string, JsonSchema4> schemas)
        {
            if (schemas != null)
            {
                foreach (var pair in schemas)
                    AddOrReplaceTypeGenerator(GetOrGenerateTypeName(pair.Value, pair.Key), new CSharpGenerator(pair.Value.ActualSchema, Settings, this));
            }
        }

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
            if (type.HasFlag(JsonObjectType.Array))
                return ResolveArray(schema);

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
                var valueType = ResolveDictionaryValueType(schema, "object", Settings.NullHandling);
                return string.Format(Settings.DictionaryType + "<string, {0}>", valueType);
            }

            return AddGenerator(schema, typeNameHint);
        }

        /// <summary>Generates all necessary classes.</summary>
        /// <returns>The code.</returns>
        public string GenerateClasses()
        {
            var classes = GenerateTypes(null);
            if (classes.Contains("JsonInheritanceConverter"))
                classes += "\n\n" + new JsonInheritanceConverterTemplate().TransformText();
            return classes;
        }

        /// <summary>Adds a generator for the given schema if necessary.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name of the created generator.</returns>
        protected override string AddGenerator(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.IsEnumeration && schema.Type == JsonObjectType.Integer)
            {
                // Recreate generator because it be better (defined enum values) than the current one
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
            return new CSharpGenerator(schema, Settings, this);
        }

        private string ResolveString(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.Format == JsonFormatStrings.DateTime)
                return isNullable ? Settings.DateTimeType + "?" : Settings.DateTimeType;

            if (schema.Format == JsonFormatStrings.TimeSpan)
                return isNullable ? "TimeSpan?" : "TimeSpan";

            if (schema.Format == JsonFormatStrings.Guid)
                return isNullable ? "Guid?" : "Guid";

#pragma warning disable 618 // used to resolve type from schemas generated with previous version of the library
            if (schema.Format == JsonFormatStrings.Base64 || schema.Format == JsonFormatStrings.Byte)
#pragma warning restore 618
                return "byte[]";

            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint);

            return "string";
        }

        private static string ResolveBoolean(bool isNullable)
        {
            return isNullable ? "bool?" : "bool";
        }

        private string ResolveInteger(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint);

            if (schema.Format == JsonFormatStrings.Byte)
                return isNullable ? "byte?" : "byte";

            if (schema.Format == JsonFormatStrings.Long)
                return isNullable ? "long?" : "long";

            return isNullable ? "int?" : "int";
        }

        private static string ResolveNumber(JsonSchema4 schema, bool isNullable)
        {
            if (schema.Format == JsonFormatStrings.Decimal)
                return isNullable ? "decimal?" : "decimal";

            return isNullable ? "double?" : "double";
        }

        private string ResolveArray(JsonSchema4 schema)
        {
            var property = schema;
            if (property.Item != null)
                return string.Format(Settings.ArrayType + "<{0}>", Resolve(property.Item, false, null));

            if (property.Items != null && property.Items.Count > 0)
                return string.Format("Tuple<" + string.Join(", ", property.Items.Select(i => Resolve(i.ActualSchema, false, null)) + ">"));

            return Settings.ArrayType + "<object>";
        }
    }
}