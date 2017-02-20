using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.CSharp;

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
            Assert.IsTrue(json.Properties["Item"].ActualPropertySchema.AllOf.First().HasSchemaReference);
        }
    }
}
