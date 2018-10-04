using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Validation
{
    public class OneOfValidationTests
    {
        private string example_schema = @"{
            ""type"": ""object"",
	        ""properties"": {
		        ""A"": {  ""type"": ""string"" },
		        ""B"": {  ""type"": ""integer"" },
		        ""C"": {  ""type"": ""number""  }
	        },
		    ""required"": [""A""],
		    ""additionalProperties"": false,
		    ""oneOf"": [
                {
                    ""required"": [""B""]
                },
			    {
                    ""required"": [""C""]
                }			
     	    ]
        }";

        [Fact]
        public async Task When_has_one_of_then_it_is_validated_correctly()
        {
            var schema = await JsonSchema4.FromJsonAsync(example_schema);
            var matches = new string[] { @"{ ""A"": ""string"", ""B"": 3  }",
                                         @"{ ""A"": ""string"", ""C"": 2 }" };

            foreach (var match in matches)
            {
                var errors = schema.Validate(match);

                Assert.Equal(0, errors.Count);
            }
        }

        [Fact]
        public async Task When_does_not_have_one_of_then_it_is_invalid()
        {
            var schema = await JsonSchema4.FromJsonAsync(example_schema);

            var errors = schema.Validate(@"{ ""A"": ""string"" }");

            Assert.Equal(1, errors.Count);
        }

        [Fact]
        public async Task When_has_more_than_one_of_then_it_is_invalid()
        {
            var schema = await JsonSchema4.FromJsonAsync(example_schema);

            var errors = schema.Validate(@"{ ""A"": ""string"", ""B"": 1, ""C"": 2 }");

            Assert.Equal(1, errors.Count);
        }
    }
}
