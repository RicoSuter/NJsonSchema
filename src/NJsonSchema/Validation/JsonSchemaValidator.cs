//-----------------------------------------------------------------------
// <copyright file="JsonSchemaValidator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// SPDX-License-Identifier: MIT
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema.Validation
{
    /// <summary>Class to validate a JSON schema against a given <see cref="JsonNode"/>. </summary>
    public class JsonSchemaValidator
    {
        private readonly Dictionary<string, IFormatValidator[]> _formatValidatorsMap;
        private readonly JsonSchemaValidatorSettings _settings;

        /// <summary>
        /// Initializes JsonSchemaValidator
        /// </summary>
        public JsonSchemaValidator(params IFormatValidator[] customValidators)
            : this(new JsonSchemaValidatorSettings() { FormatValidators = customValidators })
        {
        }

        /// <summary>
        /// Initializes JsonSchemaValidator
        /// </summary>
        public JsonSchemaValidator(JsonSchemaValidatorSettings? settings)
        {
            _settings = settings ?? new JsonSchemaValidatorSettings();
            _formatValidatorsMap = _settings.FormatValidators.GroupBy(x => x.Format).ToDictionary(v => v.Key, v => v.ToArray());
        }

        /// <summary>Validates the given JSON data.</summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <exception cref="JsonException">Could not deserialize the JSON data.</exception>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            var documentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            string jsonToParse = jsonData;
            JsonNode? jsonObject;
            try
            {
                jsonObject = JsonNode.Parse(jsonData, documentOptions: documentOptions);
            }
            catch (JsonException)
            {
                jsonToParse = Infrastructure.JsonSchemaSerialization.FixLenientJson(jsonData);
                jsonObject = JsonNode.Parse(jsonToParse, documentOptions: documentOptions);
            }

            var errors = Validate(jsonObject, schema, schemaType);

            var lineInfoMap = BuildLineInfoMap(jsonToParse);
            ApplyLineInfo(errors, lineInfoMap);

            return errors;
        }

        /// <summary>
        /// Builds a map from JSON paths to line/column positions by scanning the raw JSON with a Utf8JsonReader.
        /// Positions match Newtonsoft.Json's IJsonLineInfo convention: the reader position right after consuming
        /// the token, measured as a character offset from the line start.
        /// Two types of entries are stored:
        /// - Value entries (key = "#/path"): position past the end of the value token
        /// - Property entries (key = "#/path\x00prop"): position past the colon of the property name
        /// </summary>
        private static Dictionary<string, (int Line, int Position)> BuildLineInfoMap(string jsonData)
        {
            var map = new Dictionary<string, (int Line, int Position)>();
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            var readerOptions = new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            var reader = new Utf8JsonReader(jsonBytes, readerOptions);

            // Precompute line start BYTE offsets for quick line/position lookup.
            // Utf8JsonReader reports byte offsets; the public LinePosition is a character
            // count per Newtonsoft's IJsonLineInfo convention, so the conversion happens
            // in GetLineAndPosition using UTF-8 decoding.
            var lineStartOffsets = BuildLineStartOffsets(jsonBytes);

            var currentPath = new List<string>();
            var arrayIndexStack = new Stack<int>();
            string? pendingPropertyName = null;
            long pendingPropertyEndOffset = 0;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                    {
                        if (pendingPropertyName != null)
                        {
                            // Record property-level position (past the colon) before pushing
                            currentPath.Add(pendingPropertyName);
                            var propertyKey = BuildPathString(currentPath) + "\x00prop";
                            map[propertyKey] = GetLineAndPosition(lineStartOffsets, pendingPropertyEndOffset, jsonBytes);
                            pendingPropertyName = null;
                        }
                        else if (arrayIndexStack.Count > 0)
                        {
                            var index = arrayIndexStack.Pop();
                            currentPath.Add($"[{index}]");
                            arrayIndexStack.Push(index + 1);
                        }

                        // Record value-level position for the container (past the opening bracket)
                        var containerPath = BuildPathString(currentPath);
                        var endOffset = reader.TokenStartIndex + 1; // past '{' or '['
                        map[containerPath] = GetLineAndPosition(lineStartOffsets, endOffset, jsonBytes);

                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            arrayIndexStack.Push(0);
                        }

                        break;
                    }
                    case JsonTokenType.EndObject:
                    {
                        if (currentPath.Count > 0)
                        {
                            currentPath.RemoveAt(currentPath.Count - 1);
                        }

                        break;
                    }
                    case JsonTokenType.EndArray:
                    {
                        if (arrayIndexStack.Count > 0)
                        {
                            arrayIndexStack.Pop();
                        }

                        if (currentPath.Count > 0)
                        {
                            currentPath.RemoveAt(currentPath.Count - 1);
                        }

                        break;
                    }
                    case JsonTokenType.PropertyName:
                    {
                        pendingPropertyName = reader.GetString();
                        // Compute the byte offset past the colon after the property name.
                        // Scan forward from end of property name string to find ':'.
                        var colonOffset = FindColonAfterPropertyName(jsonBytes, reader.TokenStartIndex);
                        pendingPropertyEndOffset = colonOffset + 1; // past the ':'
                        break;
                    }
                    default:
                    {
                        // Value token (String, Number, True, False, Null)
                        var endOffset = GetTokenEndOffset(reader);

                        string valuePath;
                        if (pendingPropertyName != null)
                        {
                            currentPath.Add(pendingPropertyName);
                            valuePath = BuildPathString(currentPath);

                            // Also record property-level position
                            var propertyKey = valuePath + "\x00prop";
                            map[propertyKey] = GetLineAndPosition(lineStartOffsets, pendingPropertyEndOffset, jsonBytes);

                            currentPath.RemoveAt(currentPath.Count - 1);
                            pendingPropertyName = null;
                        }
                        else if (arrayIndexStack.Count > 0)
                        {
                            var index = arrayIndexStack.Pop();
                            currentPath.Add($"[{index}]");
                            valuePath = BuildPathString(currentPath);
                            currentPath.RemoveAt(currentPath.Count - 1);
                            arrayIndexStack.Push(index + 1);
                        }
                        else
                        {
                            valuePath = BuildPathString(currentPath);
                        }

                        map[valuePath] = GetLineAndPosition(lineStartOffsets, endOffset, jsonBytes);
                        break;
                    }
                }
            }

            return map;
        }

        private static long GetTokenEndOffset(Utf8JsonReader reader)
        {
            // Returns the byte offset past the last byte of the current token.
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.TokenStartIndex + 2 + (reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length),
                JsonTokenType.Number => reader.TokenStartIndex + (reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length),
                JsonTokenType.True => reader.TokenStartIndex + 4,
                JsonTokenType.False => reader.TokenStartIndex + 5,
                JsonTokenType.Null => reader.TokenStartIndex + 4,
                _ => reader.TokenStartIndex + 1,
            };
        }

        private static long FindColonAfterPropertyName(byte[] jsonBytes, long propertyNameTokenStart)
        {
            // PropertyName token starts at the opening '"'. Scan forward to find the closing '"',
            // then continue scanning to find the ':'.
            var index = propertyNameTokenStart + 1; // skip opening '"'
            var inEscape = false;
            // Find closing '"'
            while (index < jsonBytes.Length)
            {
                if (inEscape)
                {
                    inEscape = false;
                }
                else if (jsonBytes[index] == (byte)'\\')
                {
                    inEscape = true;
                }
                else if (jsonBytes[index] == (byte)'"')
                {
                    break; // found closing '"'
                }
                index++;
            }
            index++; // past closing '"'
            // Find ':'
            while (index < jsonBytes.Length && jsonBytes[index] != (byte)':')
            {
                index++;
            }
            return index; // position of ':'
        }

        private static List<long> BuildLineStartOffsets(byte[] jsonBytes)
        {
            var offsets = new List<long> { 0 }; // Line 1 starts at offset 0
            for (var i = 0; i < jsonBytes.Length; i++)
            {
                if (jsonBytes[i] == (byte)'\n')
                {
                    offsets.Add(i + 1); // Next line starts after '\n'
                }
            }
            return offsets;
        }

        private static (int Line, int Position) GetLineAndPosition(List<long> lineStartOffsets, long byteOffset, byte[] jsonBytes)
        {
            // Find the line containing this byte offset.
            var lineIndex = lineStartOffsets.Count - 1;
            for (var i = lineStartOffsets.Count - 1; i >= 0; i--)
            {
                if (lineStartOffsets[i] <= byteOffset)
                {
                    lineIndex = i;
                    break;
                }
            }

            var lineStart = lineStartOffsets[lineIndex];
            var line = lineIndex + 1; // 1-based line number
            var byteLength = (int)(byteOffset - lineStart);

            // Convert the byte offset within the line to a character count so LinePosition
            // matches Newtonsoft's IJsonLineInfo convention (characters, not UTF-8 bytes).
            var position = byteLength == 0
                ? 0
                : System.Text.Encoding.UTF8.GetCharCount(jsonBytes, (int)lineStart, byteLength);
            return (line, position);
        }

        private static string BuildPathString(List<string> pathSegments)
        {
            if (pathSegments.Count == 0)
            {
                return "#";
            }

            var result = new System.Text.StringBuilder("#/");
            for (var i = 0; i < pathSegments.Count; i++)
            {
                var segment = pathSegments[i];
                if (i > 0 && !segment.StartsWith('['))
                {
                    result.Append('.');
                }
                result.Append(segment);
            }

            return result.ToString();
        }

        private static void ApplyLineInfo(ICollection<ValidationError> errors, Dictionary<string, (int Line, int Position)> lineInfoMap)
        {
            foreach (var error in errors)
            {
                ApplyLineInfoToError(error, lineInfoMap);
            }
        }

        private static void ApplyLineInfoToError(ValidationError error, Dictionary<string, (int Line, int Position)> lineInfoMap)
        {
            string? lookupPath = null;

            if (error.Token is JsonNode tokenNode)
            {
                lookupPath = ConvertJsonNodePathToValidationPath(tokenNode.GetPath());
            }
            else if (error.Token is JsonPropertyToken)
            {
                // For property-level errors (e.g. NoAdditionalPropertiesAllowed), use the property position
                var propertyKey = (error.Path ?? "#") + "\x00prop";
                if (lineInfoMap.TryGetValue(propertyKey, out var propertyPosition))
                {
                    error.HasLineInfo = true;
                    error.LineNumber = propertyPosition.Line;
                    error.LinePosition = propertyPosition.Position;
                }
            }

            if (!error.HasLineInfo)
            {
                // Fallback to value position using token path or error path
                lookupPath ??= error.Path ?? "#";

                if (lineInfoMap.TryGetValue(lookupPath, out var position))
                {
                    error.HasLineInfo = true;
                    error.LineNumber = position.Line;
                    error.LinePosition = position.Position;
                }
            }

            // Recurse into child schema errors
            if (error is ChildSchemaValidationError childError)
            {
                foreach (var childErrors in childError.Errors.Values)
                {
                    ApplyLineInfo(childErrors, lineInfoMap);
                }
            }
            else if (error is MultiTypeValidationError multiError)
            {
                foreach (var childErrors in multiError.Errors.Values)
                {
                    ApplyLineInfo(childErrors, lineInfoMap);
                }
            }
        }

        private static string ConvertJsonNodePathToValidationPath(string jsonNodePath)
        {
            // JsonNode.GetPath() returns "$", "$.prop1", "$.prop4[0]", "$[0]", or "$['foo.bar']".
            // Map uses "#", "#/prop1", "#/prop4[0]", "#/[0]", "#/foo.bar" etc.
            if (jsonNodePath == "$")
            {
                return "#";
            }

            var result = new System.Text.StringBuilder("#");
            var i = 1; // skip the leading '$'
            var isFirstSegment = true;

            while (i < jsonNodePath.Length)
            {
                var c = jsonNodePath[i];
                if (c == '.')
                {
                    // Dotted property name: `.propName` — `#/propName` (first) or `#/<prev>.propName`
                    i++;
                    var end = i;
                    while (end < jsonNodePath.Length && jsonNodePath[end] != '.' && jsonNodePath[end] != '[')
                    {
                        end++;
                    }
                    result.Append(isFirstSegment ? "/" : ".");
                    result.Append(jsonNodePath, i, end - i);
                    i = end;
                    isFirstSegment = false;
                }
                else if (c == '[')
                {
                    // Either bracket-quoted property name `['foo.bar']` or array index `[0]`.
                    if (i + 1 < jsonNodePath.Length && jsonNodePath[i + 1] == '\'')
                    {
                        var closeQuote = jsonNodePath.IndexOf('\'', i + 2);
                        if (closeQuote < 0)
                        {
                            break;
                        }
                        var propName = jsonNodePath.Substring(i + 2, closeQuote - (i + 2));
                        result.Append(isFirstSegment ? "/" : ".");
                        result.Append(propName);
                        i = closeQuote + 2; // past `']`
                        isFirstSegment = false;
                    }
                    else
                    {
                        // Array index — preserve `[n]` and keep as its own segment.
                        if (isFirstSegment)
                        {
                            result.Append('/');
                            isFirstSegment = false;
                        }
                        var end = jsonNodePath.IndexOf(']', i);
                        if (end < 0)
                        {
                            break;
                        }
                        result.Append(jsonNodePath, i, end - i + 1);
                        i = end + 1;
                    }
                }
                else
                {
                    // Unexpected character — bail out so we don't produce a malformed path.
                    break;
                }
            }

            return result.ToString();
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <returns>The list of validation errors.</returns>
        public ICollection<ValidationError> Validate(JsonNode? token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
        {
            return Validate(token, schema.ActualSchema, schemaType, null, string.Empty);
        }

        /// <summary>Validates the given JSON token.</summary>
        /// <param name="token">The token.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="schemaType">The type of the schema.</param>
        /// <param name="propertyName">The current property name.</param>
        /// <param name="propertyPath">The current property path.</param>
        /// <returns>The list of validation errors.</returns>
        protected virtual ICollection<ValidationError> Validate(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath)
        {
            var errors = new List<ValidationError>();

            ValidateAnyOf(token, schema, propertyName, propertyPath, errors);
            ValidateAllOf(token, schema, propertyName, propertyPath, errors);
            ValidateOneOf(token, schema, propertyName, propertyPath, errors);
            ValidateNot(token, schema, propertyName, propertyPath, errors);
            ValidateType(token, schema, schemaType, propertyName, propertyPath, errors);
            JsonSchemaValidator.ValidateEnum(token, schema, schemaType, propertyName, propertyPath, errors);
            ValidateProperties(token, schema, schemaType, propertyName, propertyPath, errors);

            return errors;
        }

        private void ValidateType(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token == null && schema.IsNullable(schemaType))
            {
                return;
            }

            var types = GetTypes(schema).ToDictionary(t => t, ICollection<ValidationError> (t) => []);
            if (types.Count > 1)
            {
                foreach (var type in types)
                {
                    ValidateArray(token, schema, schemaType, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateString(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateNumber(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    JsonSchemaValidator.ValidateInteger(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateBoolean(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateNull(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                    ValidateObject(token, schema, type.Key, propertyName, propertyPath, (List<ValidationError>)type.Value);
                }

                // just one has to validate when multiple types are defined
                if (types.All(t => t.Value.Count > 0))
                {
                    errors.Add(new MultiTypeValidationError(
                        ValidationErrorKind.NoTypeValidates, propertyName, propertyPath, types, token, schema));
                }
            }
            else
            {
                ValidateArray(token, schema, schemaType, schema.Type, propertyName, propertyPath, errors);
                ValidateString(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateNumber(token, schema, schema.Type, propertyName, propertyPath, errors);
                JsonSchemaValidator.ValidateInteger(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateBoolean(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateNull(token, schema, schema.Type, propertyName, propertyPath, errors);
                ValidateObject(token, schema, schema.Type, propertyName, propertyPath, errors);
            }
        }

        private static IEnumerable<JsonObjectType> GetTypes(JsonSchema schema)
        {
            return JsonSchema.JsonObjectTypes.Where(t => schema.Type.HasFlag(t));
        }

        private void ValidateAnyOf(JsonNode? token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema._anyOf.Count > 0)
            {
                var propertyErrors = schema._anyOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.All(s => s.Value.Count != 0))
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAnyOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateAllOf(JsonNode? token, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema._allOf.Count > 0)
            {
                var propertyErrors = schema._allOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.Any(s => s.Value.Count != 0))
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotAllOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateOneOf(JsonNode? token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
        {
            if (schema._oneOf.Count > 0)
            {
                var propertyErrors = schema._oneOf.ToDictionary(s => s, s => Validate(token, s));
                if (propertyErrors.Count(s => s.Value.Count == 0) != 1)
                {
                    errors.Add(new ChildSchemaValidationError(ValidationErrorKind.NotOneOf, propertyName, propertyPath, propertyErrors, token, schema));
                }
            }
        }

        private void ValidateNot(JsonNode? token, JsonSchema schema, string? propertyName, string? propertyPath, List<ValidationError> errors)
        {
            if (schema.Not != null && Validate(token, schema.Not).Count == 0)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ExcludedSchemaValidates, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateNull(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsNull() && token != null)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NullExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateEnum(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.IsNullable(schemaType) && token == null)
            {
                return;
            }

            if (schema.Enumeration.Count > 0 && schema.Enumeration.All(v => v?.ToString() != token?.ToString()))
            {
                errors.Add(new ValidationError(ValidationErrorKind.NotInEnumeration, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateString(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var isString = token is JsonValue v && v.TryGetValue<string>(out _);

            if (isString)
            {
                var value = token!.GetValue<string>();

                if (value != null)
                {
                    if (!string.IsNullOrEmpty(schema.Pattern))
                    {
                        if (!Regex.IsMatch(value, schema.Pattern))
                        {
                            errors.Add(new ValidationError(ValidationErrorKind.PatternMismatch, propertyName, propertyPath, token, schema));
                        }
                    }
                    if (schema.MinLength.HasValue && value.Length < schema.MinLength)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooShort, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MaxLength.HasValue && value.Length > schema.MaxLength)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.StringTooLong, propertyName, propertyPath, token, schema));
                    }

                    if (!string.IsNullOrEmpty(schema.Format)
                        && _formatValidatorsMap.TryGetValue(schema.Format!, out var formatValidators)
                        && !formatValidators.Any(x => x.IsValid(value, JsonValueKind.String)))
                    {
                        errors.AddRange(formatValidators.Select(x => x.ValidationErrorKind).Distinct()
                            .Select(validationErrorKind => new ValidationError(validationErrorKind, propertyName, propertyPath, token, schema)));
                    }
                }
            }
            else if (type.IsString())
            {
                errors.Add(new ValidationError(ValidationErrorKind.StringExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateNumber(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var isNumber = IsNumericValue(token);
            var isInteger = IsIntegerValue(token);

            if (type.IsNumber() && !isNumber && !isInteger)
            {
                errors.Add(new ValidationError(ValidationErrorKind.NumberExpected, propertyName, propertyPath, token, schema));
            }

            if (isNumber || isInteger)
            {
                try
                {
                    var value = GetDecimalValue(token!);

                    if (schema.Minimum.HasValue && (schema.IsExclusiveMinimum ? value <= schema.Minimum : value < schema.Minimum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.Maximum.HasValue && (schema.IsExclusiveMaximum ? value >= schema.Maximum : value > schema.Maximum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMinimum.HasValue && value <= schema.ExclusiveMinimum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMaximum.HasValue && value >= schema.ExclusiveMaximum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MultipleOf.HasValue && value % schema.MultipleOf != 0)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberNotMultipleOf, propertyName, propertyPath, token, schema));
                    }
                }
                catch (OverflowException)
                {
                    var value = GetDoubleValue(token!);

                    if (schema.Minimum.HasValue && (schema.IsExclusiveMinimum ? value <= (double)schema.Minimum : value < (double)schema.Minimum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.Maximum.HasValue && (schema.IsExclusiveMaximum ? value >= (double)schema.Maximum : value > (double)schema.Maximum))
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMinimum.HasValue && value <= (double)schema.ExclusiveMinimum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooSmall, propertyName, propertyPath, token, schema));
                    }

                    if (schema.ExclusiveMaximum.HasValue && value >= (double)schema.ExclusiveMaximum)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberTooBig, propertyName, propertyPath, token, schema));
                    }

                    if (schema.MultipleOf.HasValue && value % (double)schema.MultipleOf != 0)
                    {
                        errors.Add(new ValidationError(ValidationErrorKind.NumberNotMultipleOf, propertyName, propertyPath, token, schema));
                    }
                }
            }
        }

        private static void ValidateInteger(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsInteger() && !IsIntegerValue(token))
            {
                errors.Add(new ValidationError(ValidationErrorKind.IntegerExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateBoolean(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsBoolean() && !(token is JsonValue bv && bv.TryGetValue<bool>(out _)))
            {
                errors.Add(new ValidationError(ValidationErrorKind.BooleanExpected, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateObject(JsonNode? token, JsonSchema schema, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (type.IsObject() && token is not JsonObject)
            {
                errors.Add(new ValidationError(ValidationErrorKind.ObjectExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateProperties(JsonNode? token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            var obj = token as JsonObject;
            if (obj == null && schema.Type.IsNull())
            {
                return;
            }

            var stringComparer = _settings.PropertyStringComparer;

            var schemaPropertyKeys = new HashSet<string>(schema.Properties.Keys, stringComparer);

            foreach (var propertyInfo in schema.Properties)
            {
                var newPropertyPath = GetPropertyPath(propertyPath, propertyInfo.Key);

                if (obj != null &&
                    TryGetPropertyWithStringComparer(obj, propertyInfo.Key, stringComparer, out var value))
                {
                    if (value == null && propertyInfo.Value.IsNullable(schemaType))
                    {
                        continue;
                    }

                    var propertyErrors = Validate(value, propertyInfo.Value.ActualSchema, schemaType, propertyInfo.Key, newPropertyPath);
                    errors.AddRange(propertyErrors);
                }
                else if (propertyInfo.Value.IsRequired)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, propertyInfo.Key, newPropertyPath, token, schema));
                }
            }

            // Properties may be required in a schema without being specified as a property.
            foreach (var requiredProperty in schema.RequiredProperties)
            {
                if (schemaPropertyKeys.Contains(requiredProperty))
                {
                    // The property has already been checked.
                    continue;
                }

                if (obj == null || !TryGetPropertyWithStringComparer(obj, requiredProperty, stringComparer, out _))
                {
                    var newPropertyPath = GetPropertyPath(propertyPath, requiredProperty);
                    errors.Add(new ValidationError(ValidationErrorKind.PropertyRequired, requiredProperty, newPropertyPath, token, schema));
                }
            }

            if (obj != null)
            {
                var propertyNames = obj.Select(p => p.Key).ToList();

                JsonSchemaValidator.ValidateMaxProperties(token, propertyNames, schema, propertyName, propertyPath, errors);
                JsonSchemaValidator.ValidateMinProperties(token, propertyNames, schema, propertyName, propertyPath, errors);

                var additionalPropertyNames = propertyNames.Where(p => !schemaPropertyKeys.Contains(p)).ToList();

                ValidatePatternProperties(obj, additionalPropertyNames, schema, schemaType, propertyPath, errors);
                ValidateAdditionalProperties(token, obj, additionalPropertyNames, schema, schemaType, propertyName, propertyPath, errors);
            }
        }

        private static string GetPropertyPath(string propertyPath, string propertyName)
        {
            return !string.IsNullOrEmpty(propertyPath) ? propertyPath + "." + propertyName : propertyName;
        }

        private static void ValidateMaxProperties(JsonNode? token, List<string> propertyNames, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MaxProperties > 0 && propertyNames.Count > schema.MaxProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooManyProperties, propertyName, propertyPath, token, schema));
            }
        }

        private static void ValidateMinProperties(JsonNode? token, List<string> propertyNames, JsonSchema schema, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.MinProperties > 0 && propertyNames.Count < schema.MinProperties)
            {
                errors.Add(new ValidationError(ValidationErrorKind.TooFewProperties, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidatePatternProperties(JsonObject obj, List<string> additionalPropertyNames, JsonSchema schema, SchemaType schemaType, string propertyPath, List<ValidationError> errors)
        {
            foreach (var kvp in obj)
            {
                var patternPropertySchema = schema.PatternProperties.FirstOrDefault(p => Regex.IsMatch(kvp.Key, p.Key));
                if (patternPropertySchema.Value != null)
                {
                    var propPath = GetPropertyPath(propertyPath, kvp.Key);
                    var error = TryCreateChildSchemaError(kvp.Value,
                        patternPropertySchema.Value,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, kvp.Key, propPath);

                    if (error != null)
                    {
                        errors.Add(error);
                    }

                    additionalPropertyNames.Remove(kvp.Key);
                }
            }
        }

        private void ValidateAdditionalProperties(JsonNode? token, JsonObject obj, List<string> additionalPropertyNames, JsonSchema schema, SchemaType schemaType,
            string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (schema.AdditionalPropertiesSchema != null)
            {
                foreach (var propName in additionalPropertyNames)
                {
                    var propPath = GetPropertyPath(propertyPath, propName);
                    var error = TryCreateChildSchemaError(obj[propName],
                        schema.AdditionalPropertiesSchema,
                        schemaType,
                        ValidationErrorKind.AdditionalPropertiesNotValid, propName, propPath);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
            }
            else if (!schema.AllowAdditionalProperties && additionalPropertyNames.Count > 0)
            {
                foreach (var propName in additionalPropertyNames)
                {
                    var newPropertyPath = GetPropertyPath(propertyPath, propName);
                    var propertyToken = new JsonPropertyToken(propName, obj[propName]?.DeepClone());
                    errors.Add(new ValidationError(ValidationErrorKind.NoAdditionalPropertiesAllowed, propName, newPropertyPath, propertyToken, schema));
                }
            }
        }

        private void ValidateArray(JsonNode? token, JsonSchema schema, SchemaType schemaType, JsonObjectType type, string? propertyName, string propertyPath, List<ValidationError> errors)
        {
            if (token is JsonArray array)
            {
                if (schema.MinItems > 0 && array.Count < schema.MinItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooFewItems, propertyName, propertyPath, token, schema));
                }

                if (schema.MaxItems > 0 && array.Count > schema.MaxItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooManyItems, propertyName, propertyPath, token, schema));
                }

                if (schema.UniqueItems && array.Count != array.Select(a => NormalizeJsonValue(a)).Distinct().Count())
                {
                    errors.Add(new ValidationError(ValidationErrorKind.ItemsNotUnique, propertyName, propertyPath, token, schema));
                }

                for (var index = 0; index < array.Count; index++)
                {
                    var item = array[index];

                    var propertyIndex = $"[{index}]";
                    var itemPath = !string.IsNullOrEmpty(propertyPath) ? propertyPath + propertyIndex : propertyIndex;

                    if (schema.Item != null)
                    {
                        var error = TryCreateChildSchemaError(item, schema.Item, schemaType, ValidationErrorKind.ArrayItemNotValid, propertyIndex, itemPath);
                        if (error != null)
                        {
                            errors.Add(error);
                        }
                    }

                    ValidateAdditionalItems(item, schema, schemaType, index, propertyPath, errors);
                }
            }
            else if (type.IsArray())
            {
                errors.Add(new ValidationError(ValidationErrorKind.ArrayExpected, propertyName, propertyPath, token, schema));
            }
        }

        private void ValidateAdditionalItems(JsonNode? item, JsonSchema schema, SchemaType schemaType, int index, string? propertyPath, List<ValidationError> errors)
        {
            var items = schema._items;
            if (items.Count > 0)
            {
                var propertyIndex = $"[{index}]";
                if (items.Count > index)
                {
                    var error = TryCreateChildSchemaError(
                        item,
                        items[index],
                        schemaType,
                        ValidationErrorKind.ArrayItemNotValid,
                        propertyIndex,
                        propertyPath + propertyIndex);

                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
                else if (schema.AdditionalItemsSchema != null)
                {
                    var error = TryCreateChildSchemaError(item,
                        schema.AdditionalItemsSchema,
                        schemaType,
                        ValidationErrorKind.AdditionalItemNotValid, propertyIndex, propertyPath + propertyIndex);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
                else if (!schema.AllowAdditionalItems)
                {
                    errors.Add(new ValidationError(ValidationErrorKind.TooManyItemsInTuple,
                        propertyIndex, propertyPath + propertyIndex, item, schema));
                }
            }
        }

        private ChildSchemaValidationError? TryCreateChildSchemaError(JsonNode? token, JsonSchema schema, SchemaType schemaType, ValidationErrorKind errorKind, string property, string path)
        {
            var errors = Validate(token, schema.ActualSchema, schemaType, null, path);
            if (errors.Count == 0)
            {
                return null;
            }

            var errorDictionary = new Dictionary<JsonSchema, ICollection<ValidationError>>
            {
                { schema, errors }
            };

            return new ChildSchemaValidationError(errorKind, property, path, errorDictionary, token, schema);
        }

        private static bool TryGetPropertyWithStringComparer(JsonObject obj, string propertyName, StringComparer comparer, out JsonNode? value)
        {
            if (obj.TryGetPropertyValue(propertyName, out value))
            {
                return true;
            }

            foreach (var kvp in obj)
            {
                if (comparer.Equals(propertyName, kvp.Key))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool IsNumericValue(JsonNode? token)
        {
            if (token is not JsonValue value)
            {
                return false;
            }

            return value.TryGetValue<double>(out _) ||
                   value.TryGetValue<float>(out _) ||
                   value.TryGetValue<decimal>(out _);
        }

        private static bool IsIntegerValue(JsonNode? token)
        {
            if (token is not JsonValue value)
            {
                return false;
            }

            // Check if it's an integer type
            if (value.TryGetValue<long>(out _) || value.TryGetValue<int>(out _))
            {
                return true;
            }

            // Also check if a double value is actually a whole number
            if (value.TryGetValue<double>(out var d) && d == Math.Truncate(d) && !double.IsInfinity(d))
            {
                return true;
            }

            return false;
        }

        private static decimal GetDecimalValue(JsonNode token)
        {
            if (token is JsonValue value)
            {
                if (value.TryGetValue<decimal>(out var d))
                {
                    return d;
                }

                if (value.TryGetValue<long>(out var l))
                {
                    return l;
                }

                if (value.TryGetValue<double>(out var dbl))
                {
                    return (decimal)dbl;
                }
            }

            throw new InvalidOperationException("Cannot get decimal value from token.");
        }

        private static double GetDoubleValue(JsonNode token)
        {
            if (token is JsonValue value)
            {
                if (value.TryGetValue<double>(out var d))
                {
                    return d;
                }

                if (value.TryGetValue<long>(out var l))
                {
                    return l;
                }
            }

            throw new InvalidOperationException("Cannot get double value from token.");
        }

        private static string NormalizeJsonValue(JsonNode? node)
        {
            if (node == null)
            {
                return "null";
            }

            // Normalize numbers so that 1.0, 1.00 and 1 compare as equal
            if (node is JsonValue value && value.GetValueKind() == JsonValueKind.Number)
            {
                if (value.TryGetValue<double>(out var doubleValue))
                {
                    return "n:" + doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return node.ToJsonString();
        }
    }
}
