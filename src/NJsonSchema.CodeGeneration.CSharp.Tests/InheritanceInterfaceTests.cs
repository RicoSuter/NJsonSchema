using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp.Tests;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class InheritanceInterfaceTests
    {
        public class MyContainer
        {
            public Banana Item { get; set; }
        }
        
        public interface ITour
        {
            Int64 ID { get; set; }
        }

        [DataContract]
        [JsonConverter(typeof(JsonInheritanceConverter))]
        public class Fruit : ITour
        {
            [DataMember]
            public Int64 ID { get; set; }
        }

        public interface IBanana : ITour
        {
            char Amember { get; set; }
        }

        [DataContract]
        public class Banana : Fruit, IBanana
        {
            [DataMember]
            public char Amember { get; set; }
        }

        [Fact]
        public async Task When_schema_has_base_schema_then_it_is_referenced_with_Newtonsoft()
        {
            // Arrange
            var json = NewtonsoftJsonSchemaGenerator.FromType<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings { JsonLibrary = CSharpJsonLibrary.NewtonsoftJson });

            // Act
            var code = generator.GenerateFile();

            // Assert
            Assert.True(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_schema_has_base_schema_then_it_is_referenced_with_STJ()
        {
            // Arrange
            var json = JsonSchema.FromType<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings { JsonLibrary = CSharpJsonLibrary.SystemTextJson });

            // Act
            var code = generator.GenerateFile();

            // Assert
            Assert.True(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_using_STJ_polymorphic_serialization_then_NSwag_inheritance_converter_and_attributes_are_not_generated()
        {
            // Arrange
            var json = JsonSchema.FromType<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings
            {
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
                JsonPolymorphicSerializationStyle = CSharpJsonPolymorphicSerializationStyle.SystemTextJson
            });

            // Act
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }
    }
}
