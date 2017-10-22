using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Converters;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    [TestClass]
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

        [TestMethod]
        public async Task When_schema_has_base_schema_then_it_is_referenced()
        {
            //// Arrange
            var json = await JsonSchema4.FromTypeAsync<MyContainer>();
            var data = json.ToJson();

            var generator = new CSharpGenerator(json, new CSharpGeneratorSettings());

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(json.Properties["Item"].ActualTypeSchema.AllOf.First().HasReference);
            Assert.IsTrue(code.Contains("[Newtonsoft.Json.JsonConverter(typeof(JsonInheritanceConverter), \"discriminator\")]"));
            Assert.IsTrue(code.Contains("[JsonInheritanceAttribute(\"Banana\", typeof(Banana))]"));
        }
    }
}
