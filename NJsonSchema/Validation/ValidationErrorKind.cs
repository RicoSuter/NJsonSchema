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
        NullExpected
    }
}