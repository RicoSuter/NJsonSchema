//-----------------------------------------------------------------------
// <copyright file="JsonSchemaDateAttribute.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.Annotations
{
    /// <summary>Annotation to mark a property or class as string type with format 'date'.</summary>
    public class JsonSchemaDateAttribute : JsonSchemaAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchemaAttribute"/> class.</summary>
        public JsonSchemaDateAttribute()
            : base(JsonObjectType.String)
        {
            Format = JsonFormatStrings.Date;
        }
    }
}