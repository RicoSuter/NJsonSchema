//-----------------------------------------------------------------------
// <copyright file="JsonSchemaExtensions.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.Visitors;

namespace NJsonSchema
{
    /// <summary>JSON Schema graph utilities.</summary>
    public static class JsonSchemaExtensions
    {
        /// <summary>Gets a value indicating whether this schema inherits from the given parent schema.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="parentSchema">The possible parent schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns>true or false.</returns>
        public static bool InheritsSchema(this JsonSchema4 schema, JsonSchema4 parentSchema, object rootObject)
        {
            return parentSchema != null && schema.ActualSchema
                .GetAllInheritedSchemas(rootObject)
                .Concat(new List<JsonSchema4> { schema })
                .Any(s => s.ActualSchema == parentSchema.ActualSchema) == true;
        }

        /// <summary>Gets the inherited/parent schema (most probable base schema in allOf).</summary>
        /// <remarks>Used for code generation.</remarks>
        public static JsonSchema4 GetInheritedSchema(this JsonSchema4 schema, object rootObject)
        {
            if (schema.HasReference)
                return null;

            if (schema.AllOf == null || schema.AllOf.Count == 0)
                return rootObject != null ? schema.GetOneOfInheritedSchema(rootObject) : null;

            if (schema.AllOf.Count == 1)
                return schema.AllOf.First().ActualSchema;

            if (schema.AllOf.Any(s => s.HasReference && !s.ActualSchema.IsAnyType))
                return schema.AllOf.First(s => s.HasReference && !s.ActualSchema.IsAnyType).ActualSchema;

            if (schema.AllOf.Any(s => s.Type.HasFlag(JsonObjectType.Object) && !s.ActualSchema.IsAnyType))
                return schema.AllOf.First(s => s.Type.HasFlag(JsonObjectType.Object) && !s.ActualSchema.IsAnyType).ActualSchema;

            return schema.AllOf.First(s => !s.ActualSchema.IsAnyType)?.ActualSchema ??
                (rootObject != null ? schema.GetOneOfInheritedSchema(rootObject) : null);
        }

        /// <summary>Gets the list of all inherited/parent schemas.</summary>
        /// <remarks>Used for code generation.</remarks>
#if !LEGACY
        public static IReadOnlyCollection<JsonSchema4> GetAllInheritedSchemas(this JsonSchema4 schema, object rootObject)
#else
        public static ICollection<JsonSchema4> GetAllInheritedSchemas(this JsonSchema4 schema, object rootObject)
#endif
        {
            var inheritedSchema = schema.GetInheritedSchema(rootObject) != null ?
                new List<JsonSchema4> { schema.GetInheritedSchema(rootObject) } :
                new List<JsonSchema4>();

            return inheritedSchema.Concat(inheritedSchema.SelectMany(s => s.GetAllInheritedSchemas(rootObject))).ToList();
        }

        /// <summary>Determines whether the given schema is the parent schema of this schema (i.e. super/base class).</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="inheritedSchema">The possible subtype schema.</param>
        /// <param name="rootObject">The root object needed to scan oneOf properties.</param>
        /// <returns>true or false</returns>
        public static bool Inherits(this JsonSchema4 schema, JsonSchema4 inheritedSchema, object rootObject)
        {
            inheritedSchema = inheritedSchema.ActualSchema;
            return schema.GetInheritedSchema(rootObject)?.ActualSchema == inheritedSchema || 
                schema.GetInheritedSchema(rootObject)?.Inherits(inheritedSchema, rootObject) == true;
        }

        /// <summary>Gets the discriminator or discriminator of an inherited schema (or null).</summary>
        public static OpenApiDiscriminator GetBaseDiscriminator(this JsonSchema4 schema, object rootObject)
        {
            return schema.DiscriminatorObject ?? schema.GetInheritedSchema(rootObject)?.ActualSchema.GetBaseDiscriminator(rootObject);
        }

        /// <summary>Gets the derived schemas.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static IDictionary<JsonSchema4, string> GetDerivedSchemas(this JsonSchema4 schema, object rootObject)
        {
            var visitor = new DerivedSchemaVisitor(schema, rootObject);
            visitor.VisitAsync(rootObject).GetAwaiter().GetResult();
            return visitor.DerivedSchemas;
        }

        private class DerivedSchemaVisitor : JsonSchemaVisitorBase
        {
            private readonly JsonSchema4 _baseSchema;
            private readonly object _rootObject;

            public Dictionary<JsonSchema4, string> DerivedSchemas { get; } = new Dictionary<JsonSchema4, string>();

            public DerivedSchemaVisitor(JsonSchema4 baseSchema, object rootObject)
            {
                _baseSchema = baseSchema;
                _rootObject = rootObject;
            }

#pragma warning disable 1998
            protected override async Task<JsonSchema4> VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint)
#pragma warning restore 1998
            {
                if (schema.Inherits(_baseSchema, _rootObject) && _baseSchema != schema)
                    DerivedSchemas.Add(schema, typeNameHint);

                return schema;
            }
        }


        /// <summary>Gets the schemas which are inherited via oneOf.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static JsonSchema4 GetOneOfInheritedSchema(this JsonSchema4 schema, object rootObject)
        {
            return schema.GetOneOfInheritedSchemas(rootObject)?.FirstOrDefault().Key;
        }

        /// <summary>Gets the schemas which are inherited via oneOf.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        /// <returns></returns>
        public static IDictionary<JsonSchema4, string> GetOneOfInheritedSchemas(this JsonSchema4 schema, object rootObject)
        {
            var visitor = new InheritedSchemaVisitor(schema);
            visitor.VisitAsync(rootObject).GetAwaiter().GetResult();
            return visitor.InheritedSchemas;
        }

        private class InheritedSchemaVisitor : JsonSchemaVisitorBase
        {
            private readonly JsonSchema4 _schema;

            public Dictionary<JsonSchema4, string> InheritedSchemas { get; } = new Dictionary<JsonSchema4, string>();

            public InheritedSchemaVisitor(JsonSchema4 schema)
            {
                _schema = schema;
            }

#pragma warning disable 1998
            protected override async Task<JsonSchema4> VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint)
#pragma warning restore 1998
            {
                if (schema.OneOf.Any(s => s.ActualSchema == _schema.ActualSchema))
                    InheritedSchemas.Add(schema, typeNameHint);

                return schema;
            }
        }
    }
}