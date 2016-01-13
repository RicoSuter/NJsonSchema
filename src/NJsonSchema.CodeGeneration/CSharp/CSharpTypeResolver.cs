//-----------------------------------------------------------------------
// <copyright file="CSharpTypeResolver.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>Manages the generated types and converts JSON types to CSharp types. </summary>
    public class CSharpTypeResolver : TypeResolverBase<CSharpGenerator>
    {
        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings)
        {
            Settings = settings; 
        }

        /// <summary>Initializes a new instance of the <see cref="CSharpTypeResolver"/> class.</summary>
        /// <param name="settings">The generator settings.</param>
        /// <param name="knownSchemes">The known schemes.</param>
        public CSharpTypeResolver(CSharpGeneratorSettings settings, JsonSchema4[] knownSchemes) 
            : this(settings)
        {
            foreach (var type in knownSchemes)
                AddOrReplaceTypeGenerator(type.TypeName, new CSharpGenerator(type.ActualSchema, Settings, this));
        }

        /// <summary>Gets the generator settings.</summary>
        public CSharpGeneratorSettings Settings { get; private set; }

        /// <summary>Resolves and possibly generates the specified schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isRequired">Specifies whether the given type usage is required.</param>
        /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
        /// <returns>The type name.</returns>
        public override string Resolve(JsonSchema4 schema, bool isRequired, string typeNameHint)
        {
            schema = schema.ActualSchema;

            var type = schema.Type;
            if (type.HasFlag(JsonObjectType.Array))
            {
                var property = schema;
                if (property.Item != null)
                    return string.Format("ObservableCollection<{0}>", Resolve(property.Item, true, null));
                
                throw new NotImplementedException("Array with multiple Items schemes are not supported.");
            }

            if (type.HasFlag(JsonObjectType.Number))
                return isRequired ? "decimal" : "decimal?";

            if (type.HasFlag(JsonObjectType.Integer))
            {
                if (schema.IsEnumeration)
                    return AddGenerator(schema, typeNameHint);

                if (schema.Format == JsonFormatStrings.Byte)
                    return isRequired ? "byte" : "byte?";

                return isRequired ? "long" : "long?";
            }

            if (type.HasFlag(JsonObjectType.Boolean))
                return isRequired ? "bool" : "bool?";

            if (type.HasFlag(JsonObjectType.String))
            {
                if (schema.Format == JsonFormatStrings.DateTime)
                    return isRequired ? Settings.DateTimeType.ToString() : Settings.DateTimeType + "?";

                if (schema.Format == JsonFormatStrings.TimeSpan)
                    return isRequired ? "TimeSpan" : "TimeSpan?";

                if (schema.Format == JsonFormatStrings.Guid)
                    return isRequired ? "Guid" : "Guid?";

                if (schema.Format == JsonFormatStrings.Base64)
                    return "byte[]";

                if (schema.IsEnumeration)
                    return AddGenerator(schema, typeNameHint);

                return "string";
            }

            if (type.HasFlag(JsonObjectType.Object))
            {
                if (schema.IsAnyType)
                    return "object";

                if (schema.IsDictionary)
                    return string.Format("Dictionary<string, {0}>", Resolve(schema.AdditionalPropertiesSchema, true, null));
                
                return AddGenerator(schema, typeNameHint);
            }

            throw new NotImplementedException("Type not supported");
        }

        /// <summary>Adds a generator for the given schema if necessary.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns></returns>
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
    }
}