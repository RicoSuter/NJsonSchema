using Xunit;

namespace NJsonSchema.CodeGeneration.CSharp.Tests
{
    public class CSharpJsonSerializerGeneratorTests
    {
        [Fact]
        public void When_using_SystemTextJson_GenerateJsonSerializerParameterCode_generates_correctly()
        {
            // Arrange
            var additionalJsonConverters = new string[] { "AdditionalConverter1", "AdditionalConverter2" };
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonSerializerParameterCode(settings, additionalJsonConverters);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("new System.Text.Json.JsonSerializerOptions()", output);
        }

        [Fact]
        public void When_using_NewtonsoftJson_with_JsonConverters_GenerateJsonSerializerParameterCode_generates_correctly()
        {
            // Arrange
            var additionalJsonConverters = new string[] { "AdditionalConverter1", "AdditionalConverter2" };
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
                JsonConverters = ["CustomConverter1", "CustomConverter2"]
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonSerializerParameterCode(settings, additionalJsonConverters);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("new Newtonsoft.Json.JsonConverter[] { new CustomConverter1(), new CustomConverter2(), new AdditionalConverter1(), new AdditionalConverter2() }", output);
        }

        [Fact]
        public void When_using_SystemTextJson_with_JsonSerializerSettingsOrOptionsTransformationMethod_GenerateJsonSerializerParameterCode_generates_correctly()
        {
            // Arrange
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                JsonSerializerSettingsTransformationMethod = "TestJsonSerializerSettingsTransformationMethod"
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonSerializerParameterCode(settings, null);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("TestJsonSerializerSettingsTransformationMethod(new System.Text.Json.JsonSerializerOptions())", output);
        }

        [Fact]
        public void When_using_NewtonsoftJson_with_HandleReferences_and_JsonConverters_and_JsonSerializerSettingsTransformationMethod_GenerateJsonSerializerParameterCode_generates_correctly()
        {
            // Arrange
            var additionalJsonConverters = new string[] { "AdditionalConverter1", "AdditionalConverter2" };
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
                HandleReferences = true,
                JsonConverters = ["CustomConverter1", "CustomConverter2"],
                JsonSerializerSettingsTransformationMethod = "TestJsonSerializerSettingsTransformationMethod",
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonSerializerParameterCode(settings, additionalJsonConverters);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("TestJsonSerializerSettingsTransformationMethod(new Newtonsoft.Json.JsonSerializerSettings { PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All, Converters = new Newtonsoft.Json.JsonConverter[] { new CustomConverter1(), new CustomConverter2(), new AdditionalConverter1(), new AdditionalConverter2() } })", output);
        }

        [Fact]
        public void When_using_SystemTextJson_with_JsonConverters_GenerateJsonConvertersArrayCode_generates_correctly()
        {
            // Arrange
            var additionalJsonConverters = new string[] { "AdditionalConverter1", "AdditionalConverter2" };
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                JsonConverters = ["CustomConverter1", "CustomConverter2"]
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonConvertersArrayCode(settings, additionalJsonConverters);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("new System.Text.Json.Serialization.JsonConverter[] { new CustomConverter1(), new CustomConverter2(), new AdditionalConverter1(), new AdditionalConverter2() }", output);
        }

        [Fact]
        public void When_using_NewtonsoftJson_with_JsonConverters_GenerateJsonConvertersArrayCode_generates_correctly()
        {
            // Arrange
            var additionalJsonConverters = new string[] { "AdditionalConverter1", "AdditionalConverter2" };
            var settings = new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.NewtonsoftJson,
                JsonConverters = ["CustomConverter1", "CustomConverter2"]
            };

            // Act
            var output = CSharpJsonSerializerGenerator.GenerateJsonConvertersArrayCode(settings, additionalJsonConverters);
            //Console.WriteLine(output);

            // Assert
            Assert.Equal("new Newtonsoft.Json.JsonConverter[] { new CustomConverter1(), new CustomConverter2(), new AdditionalConverter1(), new AdditionalConverter2() }", output);
        }
    }
}
