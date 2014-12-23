namespace NJsonSchema.Validation
{
    public enum ValidationErrorKind
    {
        Unknown, 
        StringExpected, 
        NumberExpected,
        IntegerExpected,
        BooleanExpected,
        ObjectExpected,
        PropertyRequired,
        ArrayExpected,
        NullExpected,
        PatternMismatch,
        StringTooShort,
        StringTooLong,
        IntegerTooSmall,
        IntegerTooBig,
        TooManyItems,
        TooFewItems,
        ItemsNotUnique,
        DateTimeExpected,

        NotAnyOf,
        NotAllOf,
        NotOneOf,

        ExcludedSchemaValidates
    }
}