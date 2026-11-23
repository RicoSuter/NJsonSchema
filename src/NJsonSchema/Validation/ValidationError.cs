//-----------------------------------------------------------------------
// <copyright file="ValidationError.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Validation
{
    /// <summary>A validation error. </summary>
    public class ValidationError
    {
        /// <summary>Initializes a new instance of the <see cref="ValidationError"/> class. </summary>
        /// <param name="errorKind">The error kind. </param>
        /// <param name="propertyName">The property name. </param>
        /// <param name="propertyPath">The property path. </param>
        /// <param name="token">The token that failed to validate. </param>
        /// <param name="schema">The schema that contains the validation rule.</param>
        public ValidationError(ValidationErrorKind errorKind, string? propertyName, string? propertyPath, JToken? token, JsonSchema schema)
        {
            Kind = errorKind;
            Property = propertyName;
            Path = propertyPath != null ? "#/" + propertyPath : "#";
            Token = token;

            var lineInfo = token as IJsonLineInfo;
            HasLineInfo = lineInfo != null && lineInfo.HasLineInfo();
            if (HasLineInfo)
            {
                LineNumber = lineInfo!.LineNumber;
                LinePosition = lineInfo.LinePosition;
            }
            else
            {
                LineNumber = 0;
                LinePosition = 0;
            }

            Schema = schema;
        }

        /// <summary>Gets the error kind. </summary>
        public ValidationErrorKind Kind { get; private set; }

        /// <summary>Gets the property name. </summary>
        public string? Property { get; private set; }

        /// <summary>Gets the property path. </summary>
        public string? Path { get; private set; }

        /// <summary>Indicates whether or not the error contains line information.</summary>
        public bool HasLineInfo { get; private set; }

        /// <summary>Gets the line number the validation failed on. </summary>
        public int LineNumber { get; private set; }

        /// <summary>Gets the line position the validation failed on. </summary>
        public int LinePosition { get; private set; }

        /// <summary>Gets the schema element that contains the validation rule. </summary>
        public JsonSchema Schema { get; private set; }

        /// <summary>Gets the JToken of the element failed the validation. </summary>
        public JToken? Token { get; private set; }

        /// <summary>Converts the error (including nested child errors) into a structured JSON object
        /// which can easily be processed or pretty printed.</summary>
        /// <returns>The JSON object.</returns>
        public virtual JObject ToJObject()
        {
            var obj = new JObject
            {
                ["kind"] = Kind.ToString(),
                ["path"] = Path
            };

            if (Property != null)
            {
                obj["property"] = Property;
            }

            if (HasLineInfo)
            {
                obj["lineNumber"] = LineNumber;
                obj["linePosition"] = LinePosition;
            }

            return obj;
        }

        /// <summary>Converts the given errors into a JSON array of structured error objects.</summary>
        /// <param name="errors">The errors.</param>
        /// <returns>The JSON array.</returns>
        public static JArray ToJArray(IEnumerable<ValidationError> errors)
        {
            return new JArray(errors.Select(e => e.ToJObject()));
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"{Kind}: {Path}";
        }
    }
}
