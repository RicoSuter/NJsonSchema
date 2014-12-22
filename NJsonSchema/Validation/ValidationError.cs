namespace NJsonSchema.Validation
{
    public class ValidationError
    {
        public ValidationError(ValidationErrorKind kind, string property, string path)
        {
            Kind = kind; 
            Property = property;
            Path = path;
        }

        public ValidationErrorKind Kind { get; private set; }

        public string Property { get; private set; }

        public string Path { get; private set; }

    }
}
