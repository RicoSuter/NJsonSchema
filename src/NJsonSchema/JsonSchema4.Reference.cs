//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using NJsonSchema.Collections;
using NJsonSchema.References;

namespace NJsonSchema
{
    public partial class JsonSchema4 : JsonReferenceBase<JsonSchema4>, IJsonReference
    {
        /// <summary>Gets the actual schema, either this or the referenced schema.</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema4 ActualSchema => GetActualSchema(new List<JsonSchema4>());

        /// <summary>Gets the type actual schema (e.g. the shared schema of a property, parameter, etc.).</summary>
        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        [JsonIgnore]
        public virtual JsonSchema4 ActualTypeSchema
        {
            get
            {
                var schema = Reference != null ? Reference : this;
                if (schema.AllOf.Count > 1 && schema.AllOf.Count(s => !s.HasReference && !s.IsDictionary) == 1)
                {
                    return schema.AllOf.First(s => !s.HasReference && !s.IsDictionary).ActualSchema;
                }

                return schema.OneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema ?? ActualSchema;
            }
        }

        /// <summary>Gets a value indicating whether this is a schema reference ($ref, <see cref="HasAllOfSchemaReference"/>, <see cref="HasOneOfSchemaReference"/> or <see cref="HasAnyOfSchemaReference"/>).</summary>
        [JsonIgnore]
        public bool HasReference => Reference != null || HasAllOfSchemaReference || HasOneOfSchemaReference || HasAnyOfSchemaReference;

        /// <summary>Gets a value indicating whether this is an allOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAllOfSchemaReference => AllOf.Count == 1 &&
                                               AllOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               AnyOf.Count == 0 &&
                                               OneOf.Count == 0 &&
                                               Properties.Count == 0 &&
                                               PatternProperties.Count == 0 &&
                                               AllowAdditionalProperties &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <summary>Gets a value indicating whether this is an oneOf schema reference.</summary>
        [JsonIgnore]
        public bool HasOneOfSchemaReference => OneOf.Count == 1 &&
                                               OneOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               AnyOf.Count == 0 &&
                                               AllOf.Count == 0 &&
                                               Properties.Count == 0 &&
                                               PatternProperties.Count == 0 &&
                                               AllowAdditionalProperties &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <summary>Gets a value indicating whether this is an anyOf schema reference.</summary>
        [JsonIgnore]
        public bool HasAnyOfSchemaReference => AnyOf.Count == 1 &&
                                               AnyOf.Any(s => s.HasReference) &&
                                               Type == JsonObjectType.None &&
                                               AllOf.Count == 0 &&
                                               OneOf.Count == 0 &&
                                               Properties.Count == 0 &&
                                               PatternProperties.Count == 0 &&
                                               AllowAdditionalProperties &&
                                               AdditionalPropertiesSchema == null &&
                                               MultipleOf == null &&
                                               IsEnumeration == false;

        /// <exception cref="InvalidOperationException">Cyclic references detected.</exception>
        /// <exception cref="InvalidOperationException">The schema reference path has not been resolved.</exception>
        private JsonSchema4 GetActualSchema(IList<JsonSchema4> checkedSchemas)
        {
            if (checkedSchemas.Contains(this))
                throw new InvalidOperationException("Cyclic references detected.");

            if (((IJsonReferenceBase)this).ReferencePath != null && Reference == null)
                throw new InvalidOperationException("The schema reference path '" + ((IJsonReferenceBase)this).ReferencePath + "' has not been resolved.");

            if (HasReference)
            {
                checkedSchemas.Add(this);

                if (HasAllOfSchemaReference)
                    return AllOf.First().GetActualSchema(checkedSchemas);

                if (HasOneOfSchemaReference)
                    return OneOf.First().GetActualSchema(checkedSchemas);

                if (HasAnyOfSchemaReference)
                    return AnyOf.First().GetActualSchema(checkedSchemas);

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
        public override JsonSchema4 Reference
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
