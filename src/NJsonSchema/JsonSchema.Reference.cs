//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NJsonSchema.References;

namespace NJsonSchema
{
    public partial class JsonSchema : JsonReferenceBase<JsonSchema>, IJsonReference
    {
        /// <summary>Gets the actual schema, either this or the referenced schema.</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema ActualSchema
        {
            get
            {
                var checkedSchemas = new CheckedSchemaContainer();
                return GetActualSchema(ref checkedSchemas);
            }
        }

        /// <summary>Gets the type actual schema (e.g. the shared schema of a property, parameter, etc.).</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema ActualTypeSchema
        {
            get
            {
                var schema = Reference != null ? Reference : this;
                if (schema._allOf.Count > 1 && schema._allOf.Count(static s => !s.HasReference && !s.IsDictionary) == 1)
                {
                    return schema._allOf.First(static s => !s.HasReference && !s.IsDictionary).ActualSchema;
                }

                return schema._oneOf.FirstOrDefault(static o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema ?? ActualSchema;
            }
        }

        /// <summary>Gets a value indicating whether this is a schema reference ($ref, <see cref="HasAllOfSchemaReference"/>, <see cref="HasOneOfSchemaReference"/> or <see cref="HasAnyOfSchemaReference"/>).</summary>
        [JsonIgnore]
        public bool HasReference => Reference != null || HasAllOfSchemaReference || HasOneOfSchemaReference || HasAnyOfSchemaReference;

        /// <summary>Gets a value indicating whether this is an allOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAllOfSchemaReference => Type == JsonObjectType.None &&
                                               _anyOf.Count == 0 &&
                                               _oneOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               !IsEnumeration &&
                                               _allOf.Count == 1 &&
                                               _allOf.Any(s => s.HasReference);

        /// <summary>Gets a value indicating whether this is an oneOf schema reference.</summary>
        [JsonIgnore]
        public bool HasOneOfSchemaReference => Type == JsonObjectType.None &&
                                               _anyOf.Count == 0 &&
                                               _allOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               !IsEnumeration &&
                                               _oneOf.Count == 1 &&
                                               _oneOf.Any(s => s.HasReference);

        /// <summary>Gets a value indicating whether this is an anyOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAnyOfSchemaReference => Type == JsonObjectType.None &&
                                               _allOf.Count == 0 &&
                                               _oneOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               !IsEnumeration &&
                                               _anyOf.Count == 1 &&
                                               _anyOf.Any(s => s.HasReference);

        // more efficient holder for schema checks
        private struct CheckedSchemaContainer
        {
            private int _count;
            private JsonSchema _schema1;
            private JsonSchema _schema2;
            private List<JsonSchema> _schemas;

            public void Add(JsonSchema schema)
            {
                if (_count == 0)
                {
                    _schema1 = schema;
                }
                else if (_count == 1)
                {
                    _schema2 = schema;
                }
                else
                {
                    _schemas ??= new List<JsonSchema>(3) { _schema1, _schema2 };
                    _schemas.Add(schema);
                }
                _count++;
            }

            public readonly bool Contains(JsonSchema schema)
            {
                return _count > 0 &&  (ReferenceEquals(_schema1, schema) || ReferenceEquals(_schema2, schema) || _schemas?.Contains(schema) == true);
            }
        }

        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonSchema GetActualSchema(ref CheckedSchemaContainer checkedSchemas)
        {
            static void ThrowInvalidOperationException(string message)
            {
                throw new InvalidOperationException(message);
            }

            if (checkedSchemas.Contains(this))
            {
                ThrowInvalidOperationException("Cyclic references detected.");
            }

            if (Reference == null && ((IJsonReferenceBase)this).ReferencePath != null)
            {
                ThrowInvalidOperationException("The schema reference path '" + ((IJsonReferenceBase)this).ReferencePath + "' has not been resolved.");
            }

            if (HasReference)
            {
                return GetActualSchemaReferences(ref checkedSchemas) ?? this;
            }

            return this;
        }

        private JsonSchema? GetActualSchemaReferences(ref CheckedSchemaContainer checkedSchemas)
        {
            checkedSchemas.Add(this);

            if (HasAllOfSchemaReference)
            {
                return _allOf[0].GetActualSchema(ref checkedSchemas);
            }

            if (HasOneOfSchemaReference)
            {
                return _oneOf[0].GetActualSchema(ref checkedSchemas);
            }

            if (HasAnyOfSchemaReference)
            {
                return _anyOf[0].GetActualSchema(ref checkedSchemas);
            }

            return Reference?.GetActualSchema(ref checkedSchemas);
        }

        #region Implementation of IJsonReference

        /// <summary>Gets the actual referenced object, either this or the reference object.</summary>
        [JsonIgnore]
        IJsonReference IJsonReference.ActualObject => this.ActualSchema;

        /// <summary>Gets the parent object of this object. </summary>
        [JsonIgnore]
        object? IJsonReference.PossibleRoot => Parent;

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        public override JsonSchema? Reference
        {
            get => base.Reference;
            set
            {
                base.Reference = value;
                if (value != null)
                {
                    // only $ref property is allowed when schema is a reference
                    // TODO: Fix all SchemaReference assignments so that this code is not needed
                    Type = JsonObjectType.None;
                }
            }
        }

        #endregion
    }
}
