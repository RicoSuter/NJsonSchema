using NJsonSchema.Validation.FormatValidators;
using System.Collections.Generic;

namespace NJsonSchema.Validation
{
    /// <summary>
    /// Settings for JsonSChemaValidtor
    /// </summary>
    public class JsonSchemaValidatorSettings
    {
        /// <summary>
        /// Format validators collection with default format validators. 
        /// Custom fromat validtors implementing <see cref="IFormatValidator"/> can be added.
        /// </summary>
        public ICollection<IFormatValidator> FormatValidators { get; } = new List<IFormatValidator>
        {
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
            new Base64FormatValidator()
        };

        /// <summary>
        /// Returns default json schema validator settings
        /// </summary>
        public static JsonSchemaValidatorSettings Default => new JsonSchemaValidatorSettings();
    }
}
