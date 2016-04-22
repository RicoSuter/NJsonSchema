using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ClassGenerationTests
    {
        public class MyClassTest
        {
            public string Name { get; set; }

            public DateTime DateOfBirth { get; set; }

            public int[] PrimitiveArray { get; set; }

            public Dictionary<string, int> PrimitiveDictionary { get; set; }

            public DateTime[] DateArray { get; set; }

            public Dictionary<string, DateTime> DateDictionary { get; set; }

            public MyItemClass Reference { get; set; }

            public MyItemClass[] Array { get; set; }

            public Dictionary<string, MyItemClass> Dictionary { get; set; }
        }

        public class MyItemClass
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [TestMethod]
        public void Todo()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<MyClassTest>();
            var data = schema.ToJson();
            var settings = new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, settings);
            var code = generator.GenerateFile();

            //// Assert
            //Assert.IsTrue(code.Contains("Test?: { [key: string] : any; };"));
        }
    }
}