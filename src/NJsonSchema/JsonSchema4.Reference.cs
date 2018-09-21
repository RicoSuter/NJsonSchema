//-----------------------------------------------------------------------
// <copyright file="JsonSchema4.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
        public virtual JsonSchema4 ActualTypeSchema => OneOf.FirstOrDefault(o => !o.IsNullable(SchemaType.JsonSchema))?.ActualSchema ?? ActualSchema;

        /// <summary>Gets a value indicating whether this is a schema reference ($ref or <see cref="HasAllOfSchemaReference"/>).</summary>
        [JsonIgnore]
        public bool HasReference => Reference != null || HasAllOfSchemaReference;

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

        /// <summary>Gets or sets the type reference.</summary>
        [JsonIgnore]
        [Obsolete("Use the Reference property instead.")]
        public JsonSchema4 SchemaReference
        {
            get => Reference;
            set => Reference = value;
        }

        /// <summary>Gets a value indicating whether this is a schema reference ($ref or <see cref="HasAllOfSchemaReference"/>).</summary>
        [JsonIgnore]
        [Obsolete("Use the HasReference property instead.")]
        public bool HasSchemaReference => HasReference;

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
