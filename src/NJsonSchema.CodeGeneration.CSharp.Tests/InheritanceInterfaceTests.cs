using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

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
            //// Arrange
            var json = NewtonsoftJsonSchemaGenerator.FromType<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings { JsonLibrary = CSharpJsonLibrary.NewtonsoftJson });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.True(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            Assert.Contains("[Newtonsoft.Json.JsonConverter(typeof(JsonInheritanceConverter), \"discriminator\")]", code);
            Assert.Contains("[JsonInheritanceAttribute(\"Banana\", typeof(Banana))]", code);
            Assert.Contains("public class JsonInheritanceConverter : Newtonsoft.Json.JsonConverter", code);
        }

        [Fact]
        public async Task When_schema_has_base_schema_then_it_is_referenced_with_STJ()
        {
            //// Arrange
            var json = JsonSchema.FromType<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings { JsonLibrary = CSharpJsonLibrary.SystemTextJson });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.True(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            Assert.Contains("[JsonInheritanceConverter(typeof(Fruit), \"discriminator\")]", code);
            Assert.Contains("[JsonInheritanceAttribute(\"Banana\", typeof(Banana))]", code);
            Assert.Contains("public class JsonInheritanceConverter<TBase> : System.Text.Json.Serialization.JsonConverter<TBase>", code);
        }
    }
}
