namespace NJsonSchema.Validation
{
    /// <summary>Enumeration of the possible error kinds. </summary>
    public enum ValidationErrorKind
    {
        Unknown, 

        /// <summary>A string is expected. </summary>
        StringExpected,

        /// <summary>A number is expected. </summary>
        NumberExpected,

        /// <summary>An integer is expected. </summary>
        IntegerExpected,

        /// <summary>A boolean is expected. </summary>
        BooleanExpected,

        /// <summary>An object is expected. </summary>
        ObjectExpected,

        /// <summary>The property is required but not found. </summary>
        PropertyRequired,

        /// <summary>An array is expected. </summary>
        ArrayExpected,

        /// <summary>An array is expected. </summary>
        NullExpected,

        /// <summary>The Regex pattern does not match. </summary>
        PatternMismatch,

        /// <summary>The string is too short. </summary>
        StringTooShort,

        /// <summary>The string is too long. </summary>
        StringTooLong,

        /// <summary>The number is too small. </summary>
        NumberTooSmall,

        /// <summary>The number is too big. </summary>
        NumberTooBig,

        /// <summary>The integer is too small. </summary>
        IntegerTooSmall,

        /// <summary>The integer is too big. </summary>
        IntegerTooBig,

        /// <summary>The array contains too many items. </summary>
        TooManyItems,

        /// <summary>The array contains too few items. </summary>
        TooFewItems,

        /// <summary>The items in the array are not unique. </summary>
        ItemsNotUnique,

        /// <summary>A date time is expected. </summary>
        DateTimeExpected,

        /// <summary>The object is not any of the given schemas. </summary>
        NotAnyOf,

        /// <summary>The object is not all of the given schemas. </summary>
        NotAllOf,

        /// <summary>The object is not one of the given schemas. </summary>
        NotOneOf,

        /// <summary>The object matches the not allowed schema. </summary>
        ExcludedSchemaValidates,

        /// <summary>The number is not a multiple of the given number. </summary>
        NumberNotMultipleOf,

        /// <summary>The integer is not a multiple of the given integer. </summary>
        IntegerNotMultipleOf
    }
}