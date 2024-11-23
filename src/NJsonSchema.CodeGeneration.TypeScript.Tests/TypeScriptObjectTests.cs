using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class TypeScriptObjectTests
    {
        public class ObjectTest
        {
            public object Test { get; set; }
        }

        [Fact]
        public void When_property_is_object_then_jsonProperty_has_no_reference_and_is_any()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ObjectTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("Test: any;", code);
        }

        public class DictionaryObjectTest
        {
            public IDictionary<string, object> Test { get; set; }
        }

        [Fact]
        public void When_dictionary_value_is_object_then_typescript_uses_any()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DictionaryObjectTest>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("Test: { [key: string]: any; };", code);
        }
    }
}
