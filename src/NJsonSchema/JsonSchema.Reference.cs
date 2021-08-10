//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        public virtual JsonSchema ActualSchema => GetActualSchema(new List<JsonSchema>());

        /// <summary>Gets the type actual schema (e.g. the shared schema of a property, parameter, etc.).</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema ActualTypeSchema
        {
            get
            {
                var schema = Reference != null ? Reference : this;
                if (schema._allOf.Count > 1 && schema._allOf.Count(s => !s.HasReference && !s.IsDictionary) == 1)
                {
                    return schema._allOf.First(s => !s.HasReference && !s.IsDictionary).ActualSchema;
                }

                return schema._oneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema ?? ActualSchema;
            }
        }

        /// <summary>Gets a value indicating whether this is a schema reference ($ref, <see cref="HasAllOfSchemaReference"/>, <see cref="HasOneOfSchemaReference"/> or <see cref="HasAnyOfSchemaReference"/>).</summary>
        [JsonIgnore]
        public bool HasReference => Reference != null || HasAllOfSchemaReference || HasOneOfSchemaReference || HasAnyOfSchemaReference;

        /// <summary>Gets a value indicating whether this is an allOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAllOfSchemaReference => _allOf.Count == 1 &&
                                               _allOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               _anyOf.Count == 0 &&
                                               _oneOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <summary>Gets a value indicating whether this is an oneOf schema reference.</summary>
        [JsonIgnore]
        public bool HasOneOfSchemaReference => _oneOf.Count == 1 &&
                                               _oneOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               _anyOf.Count == 0 &&
                                               _allOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <summary>Gets a value indicating whether this is an anyOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAnyOfSchemaReference => _anyOf.Count == 1 &&
                                               _anyOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               _allOf.Count == 0 &&
                                               _oneOf.Count == 0 &&
                                               _properties.Count == 0 &&
                                               _patternProperties.Count == 0 &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        private JsonSchema GetActualSchema(IList<JsonSchema> checkedSchemas)
        {
            if (checkedSchemas.Contains(this))
            {
                throw new InvalidOperationException("Cyclic references detected.");
            }

            if (((IJsonReferenceBase)this).ReferencePath != null && Reference == null)
            {
                throw new InvalidOperationException("The schema reference path '" + ((IJsonReferenceBase)this).ReferencePath + "' has not been resolved.");
            }

            if (HasReference)
            {
                checkedSchemas.Add(this);

                if (HasAllOfSchemaReference)
                {
                    return _allOf.First().GetActualSchema(checkedSchemas);
                }

                if (HasOneOfSchemaReference)
                {
                    return _oneOf.First().GetActualSchema(checkedSchemas);
                }

                if (HasAnyOfSchemaReference)
                {
                    return _anyOf.First().GetActualSchema(checkedSchemas);
                }

                return Reference.GetActualSchema(checkedSchemas);
            }

            return this;
        }

        #region Implementation of IJsonReference

        /// <summary>Gets the actual referenced object, either this or the reference object.</summary>
        [JsonIgnore]
        IJsonReference IJsonReference.ActualObject => this.ActualSchema;

        /// <summary>Gets the parent object of this object. </summary>
        [JsonIgnore]
        object IJsonReference.PossibleRoot => Parent;

        /// <summary>Gets or sets the referenced object.</summary>
        [JsonIgnore]
        public override JsonSchema Reference
        {
            get { return base.Reference; }
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
