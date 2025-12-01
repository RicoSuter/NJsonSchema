using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation
{
    public class DefaultSchemaNameGeneratorTests
    {
        [Theory]
        [InlineData(typeof(Dictionary<string, int>), "DictionaryOfStringAndInteger")]
        [InlineData(typeof(Dictionary<bool, long>), "DictionaryOfBooleanAndLong")]
        [InlineData(typeof(Dictionary<decimal, short>), "DictionaryOfDecimalAndShort")]
        [InlineData(typeof(Dictionary<Guid, DateTime>), "DictionaryOfGuidAndDateTime")]
        [InlineData(typeof(Dictionary<decimal?, short?>), "DictionaryOfNullableDecimalAndNullableShort")]
        public void When_display_name_is_retrieved_then_string_is_correct(Type type, string expectedName)
        {
            // Act
            var generator = new DefaultSchemaNameGenerator();
            var name = generator.Generate(type);

            // Assert
            Assert.Equal(expectedName, name);
        }
    }
}
