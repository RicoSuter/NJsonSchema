using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class DefaultPropertyTests
    {
        [Fact]
        public async Task When_property_has_interger_default_it_is_reflected_in_the_poco()
        {
            var data = @"{'properties': {
                                'intergerWithDefault': {      
                                    'type': 'integer',
                                    'format': 'int32',
                                    'default': 5
                                 }
                             }}";

            var schema = await JsonSchema4.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = true
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public int IntergerWithDefault { get; set; } = 5;", output);
        }

        [Fact]
        public async Task When_property_has_boolean_default_it_is_reflected_in_the_poco()
        {
            var data = @"{'properties': {
                                'boolWithDefault': {
                                    'type': 'boolean',
                                    'default': false
                                 }
                             }}";

            var schema = await JsonSchema4.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = true
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public bool BoolWithDefault { get; set; } = false;", output);
        }

        [Fact]
        public async Task When_property_has_boolean_default_and_default_value_generation_is_disabled_then_default_value_is_not_generated()
        {
            var data = @"{'properties': {
                                'boolWithDefault': {
                                    'type': 'boolean',
                                    'default': false
                                 }
                             }}";

            var schema = await JsonSchema4.FromJsonAsync(data);
            var settings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                Namespace = "ns",
                GenerateDefaultValues = false
            };
            var gen = new CSharpGenerator(schema, settings);
            var output = gen.GenerateFile("MyClass");

            Assert.Contains("public bool BoolWithDefault { get; set; }", output);
            Assert.DoesNotContain("public bool BoolWithDefault { get; set; } = false;", output);
        }
    }
}
