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
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class TypeScriptTypeResolver : TypeResolverBase<TypeScriptGenerator>
    {
        private readonly object _rootObject;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="rootObject">The root object to search for JSON Schemas.</param>
        public TypeScriptTypeResolver(TypeScriptGeneratorSettings settings, object rootObject)
            : this(settings, rootObject, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <param name="rootObject">The root object.</param>
        /// <param name="knownSchemes">The known schemes.</param>
        public TypeScriptTypeResolver(TypeScriptGeneratorSettings settings, object rootObject, JsonSchema4[] knownSchemes)
            : base(settings.TypeNameGenerator)
        {
            _rootObject = rootObject;
            Settings = settings;

            if (knownSchemes != null)
            {
                // TODO: Only relies on type name raw => find better way...
                foreach (var type in knownSchemes)
                    AddOrReplaceTypeGenerator(type.GetTypeName(settings.TypeNameGenerator), new TypeScriptGenerator(type.ActualSchema, settings, this, rootObject));
            }
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; private set; }

        /// <summary>Gets or sets the namespace of the generated classes.</summary>
        public string Namespace { get; set; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
        public override string Resolve(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            schema = schema.ActualSchema;

            if (schema.IsAnyType)
                return "any";

            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
                return ResolveArray(schema, typeNameHint);

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
                var valueType = ResolveDictionaryValueType(schema, "any", Settings.NullHandling);
                return $"{{ [key: string] : {valueType}; }}";
            }

            return AddGenerator(schema, typeNameHint);
        }

        /// <summary>Creates a type generator.</summary>
        /// <param name="schema">The schema.</param>
        /// <returns>The generator.</returns>
        protected override TypeScriptGenerator CreateTypeGenerator(JsonSchema4 schema)
        {
            return new TypeScriptGenerator(schema, Settings, this, _rootObject);
        }

        /// <summary>Gets or generates the type name for the given schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public override string GetOrGenerateTypeName(JsonSchema4 schema, string typeNameHint)
        {
            var typeName = base.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration && schema.Type == JsonObjectType.Integer)
                return typeName + "AsInteger";

            return typeName;
        }

        private string ResolveString(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.Format == JsonFormatStrings.DateTime && Settings.DateTimeType != TypeScriptDateTimeType.String)
                return Settings.DateTimeType == TypeScriptDateTimeType.Date ? "Date" : "moment.Moment";

            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint);

            return "string";
        }

        private string ResolveInteger(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.IsEnumeration)
                return AddGenerator(schema, typeNameHint);

            return "number";
        }

        private string ResolveArray(JsonSchema4 schema, string typeNameHint)
        {
            if (schema.Item != null)
                return string.Format("{0}[]", Resolve(schema.Item, true, typeNameHint)); // TODO: Make typeNameHint singular if possible

            if (schema.Items != null && schema.Items.Count > 0)
                return string.Format("[" + string.Join(", ", schema.Items.Select(i => Resolve(i.ActualSchema, false, null))) + "]");

            return "any[]";
        }
    }
}