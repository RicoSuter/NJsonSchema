using NJsonSchema.Validation.FormatValidators;

namespace NJsonSchema.Validation
{
    /// <summary>Class to configure the behavior of <see cref="JsonSchemaValidator"/>. </summary>
    public class JsonSchemaValidatorSettings
    {
        /// <summary>Gets or sets the <see cref="StringComparer"/> used to compare object properties.</summary>
        public StringComparer PropertyStringComparer { get; set; } = StringComparer.Ordinal;

        /// <summary>Gets or sets the format validators.</summary>
        public IEnumerable<IFormatValidator> FormatValidators { get; set; } =
        [
            new DateTimeFormatValidator(),
            new DateFormatValidator(),
            new EmailFormatValidator(),
            new GuidFormatValidator(),
            new HostnameFormatValidator(),
            new IpV4FormatValidator(),
            new IpV6FormatValidator(),
            new TimeFormatValidator(),
            new TimeSpanFormatValidator(),
            new UriFormatValidator(),
            new ByteFormatValidator(),
            new Base64FormatValidator(),
            new UuidFormatValidator()
        ];

        /// <summary>
        /// Adds a custom format validator to the <see cref="FormatValidators"/> array.
        /// </summary>
        /// <param name="formatValidator">The format validator.</param>
        public void AddCustomFormatValidator(IFormatValidator formatValidator)
        {
            FormatValidators = this
                .FormatValidators
                .Union([formatValidator])
                .ToArray();
        }
    }
}
