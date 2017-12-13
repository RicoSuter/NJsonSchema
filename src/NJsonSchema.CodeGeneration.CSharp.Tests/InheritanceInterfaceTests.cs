using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Converters;
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
        public async Task When_schema_has_base_schema_then_it_is_referenced()
        {
            //// Arrange
            var json = await JsonSchema4.FromTypeAsync<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings());

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.True(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            Assert.Contains("[Newtonsoft.Json.JsonConverter(typeof(JsonInheritanceConverter), \"discriminator\")]", code);
            Assert.Contains("[JsonInheritanceAttribute(\"Banana\", typeof(Banana))]", code);
        }
    }
}
