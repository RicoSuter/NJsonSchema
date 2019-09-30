using NJsonSchema.Validation;
using NJsonSchema.Validation.FormatValidators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class JsonSchemaValidatorSettingsTests
    {
        [Fact]
        public void When_a_default_instance_created_then_it_should_have_correct_default_format_validators()
        {
            //// Arrange 
            var expectedValidatorTypes = new[]
            {
                typeof(DateTimeFormatValidator),
                typeof(DateFormatValidator),
                typeof(EmailFormatValidator),
                typeof(GuidFormatValidator),
                typeof(HostnameFormatValidator),
                typeof(IpV4FormatValidator),
                typeof(IpV6FormatValidator),
                typeof(TimeFormatValidator),
                typeof(TimeSpanFormatValidator),
                typeof(UriFormatValidator),
                typeof(ByteFormatValidator),
                typeof(Base64FormatValidator)
            };

            ////Act
            var settings = JsonSchemaValidatorSettings.Default;


            //// Assert
            Assert.Equal(settings.FormatValidators.Select(v => v.GetType()), expectedValidatorTypes);
        }
    }
}
