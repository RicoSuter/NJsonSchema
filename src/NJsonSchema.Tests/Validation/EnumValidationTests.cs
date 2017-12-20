using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class EnumValidationTests
    {
        [Fact]
        public async Task When_enum_is_defined_without_type_then_validation_succeeds_for_correct_value()
        {
            //// Arrange
            var json =
            @"{
                ""enum"": [
                    ""commercial"",
                    ""residential""
                ]
            }";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"""commercial""");

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public async Task When_enum_is_defined_without_type_then_validation_fails_for_wrong_value()
        {
            //// Arrange
            var json =
            @"{
                ""enum"": [
                    ""commercial"",
                    ""residential""
                ]
            }";
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"""wrong""");

            //// Assert
            Assert.Equal(1, errors.Count);
        }

        [Fact]
        public async Task When_enumeration_has_null_then_validation_works()
        {
            //// Arrange
            var json = @"
            {
                ""properties"": {
                    ""SalutationType"": {
                        ""type"": [
                            ""string"",
                            ""null""
                        ],
                        ""enum"": [
                            ""Mr"",
                            ""Mrs"",
                            ""Dr"",
                            ""Ms"",
                            null
                        ]
                    }
                }
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"{ ""SalutationType"": ""Prof"" }");

            //// Assert
            Assert.Equal(1, errors.Count);
        }

        [Fact]
        public async Task When_enumeration_has_null_and_value_is_null_then_no_validation_errors()
        {
            //// Arrange
            var json = @"
            {
                ""properties"": {
                    ""SalutationType"": {
                        ""type"": [
                            ""string"",
                            ""null""
                        ],
                        ""enum"": [
                            ""Mr"",
                            ""Mrs"",
                            ""Dr"",
                            ""Ms"",
                            null
                        ]
                    }
                }
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"{ ""SalutationType"": null }");

            //// Assert
            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public async Task When_enumeration_doesnt_have_null_and_value_is_null_then_validation_fails()
        {
            //// Arrange
            var json = @"
            {
                ""properties"": {
                    ""SalutationType"": {
                        ""type"": [
                            ""string"",
                            ""null""
                        ],
                        ""enum"": [
                            ""Mr"",
                            ""Mrs"",
                            ""Dr"",
                            ""Ms""
                        ]
                    }
                }
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Act
            var errors = schema.Validate(@"{ ""SalutationType"": null }");

            //// Assert
            Assert.Equal(1, errors.Count);
        }
    }
}
