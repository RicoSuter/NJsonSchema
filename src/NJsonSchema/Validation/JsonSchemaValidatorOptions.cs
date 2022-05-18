using System;

namespace NJsonSchema.Validation
{
    /// <summary>Class to configure the behavior of <see cref="JsonSchemaValidator"/>. </summary>
    public class JsonSchemaValidatorSettings
    {
        private StringComparer _propertyStringComparer;

        /// <summary>The <see cref="StringComparer"/> used to compare object properties.</summary>
        public StringComparer PropertyStringComparer
        {
            get => _propertyStringComparer ?? StringComparer.Ordinal;
            set => _propertyStringComparer = value;
        }
    }
}
