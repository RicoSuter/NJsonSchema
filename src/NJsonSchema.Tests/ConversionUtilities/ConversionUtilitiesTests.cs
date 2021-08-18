using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.ConversionUtilities
{
    public class ConversionUtilitiesTests
    {
        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Flat_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToFlatCase(data.Input);

            //// Assert
            Assert.Equal(data.FlatCase, actual);
        }

        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Upper_Flat_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToUpperFlatCase(data.Input);

            //// Assert
            Assert.Equal(data.UpperFlatCase, actual);
        }

        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Camel_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToCamelCase(data.Input);

            //// Assert
            Assert.Equal(data.CamelCase, actual);
        }

        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Pascal_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToPascalCase(data.Input);

            //// Assert
            Assert.Equal(data.PascalCase, actual);
        }

        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Snake_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToSnakeCase(data.Input);

            //// Assert
            Assert.Equal(data.SnakeCase, actual);
        }

        [Theory]
        [MemberData(nameof(GetTestCaseData))]
        public async Task Pascal_Snake_Case_Data_Should_Match(TestCaseData data)
        {
            //// Act
            var actual = NJsonSchema.ConversionUtilities.ConvertNameToPascalSnakeCase(data.Input);

            //// Assert
            Assert.Equal(data.PascalSnakeCase, actual);
        }

        public static IEnumerable<object[]> GetTestCaseData()
        {
            yield return new[] { new TestCaseData("TwoWords", "twowords", "TWOWORDS", "twoWords", "TwoWords", "two_words", "Two_Words") };

            yield return new[] { new TestCaseData("Two_Words", "twowords", "TWOWORDS", "twoWords", "TwoWords", "two_words", "Two_Words") };

            yield return new[] { new TestCaseData("twoWords", "twowords", "TWOWORDS", "twoWords", "TwoWords", "two_words", "Two_Words") };

            yield return new[] { new TestCaseData("two_words", "twowords", "TWOWORDS", "twoWords", "TwoWords", "two_words", "Two_Words") };

            yield return new[] { new TestCaseData("two_words", "twowords", "TWOWORDS", "twoWords", "TwoWords", "two_words", "Two_Words") };

            yield return new[] { new TestCaseData("class", "@class", "CLASS", "@class", "Class", "@class", "Class") };

            yield return new[] { new TestCaseData("%yield", "yield", "YIELD", "yield", "Yield", "yield", "Yield") };

            yield return new[] { new TestCaseData("2+", "_2plus", "_2PLUS", "_2plus", "_2plus", "_2plus", "_2plus") };
        }

        public class TestCaseData
        {
            public string Input { get; private set; }

            public string FlatCase { get; private set; }

            public string UpperFlatCase { get; private set; }

            public string CamelCase { get; private set; }

            public string PascalCase { get; private set; }

            public string SnakeCase { get; private set; }

            public string PascalSnakeCase { get; private set; }

            public TestCaseData(string input, string flatCase, string upperFlatCase, string camelCase, string pascalCase, string snakeCase, string pascalSnakeCase)
            {
                Input = input;
                FlatCase = flatCase;
                UpperFlatCase = upperFlatCase;
                CamelCase = camelCase;
                PascalCase = pascalCase;
                SnakeCase = snakeCase;
                PascalSnakeCase = pascalSnakeCase;
            }
        }
    }
}
