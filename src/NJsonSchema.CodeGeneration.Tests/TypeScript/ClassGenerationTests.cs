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

            public Student Reference { get; set; }

            public Student[] Array { get; set; }

            public Dictionary<string, Student> Dictionary { get; set; }
        }

        public class Student : Person
        {
            public string Study { get; set; }
        }

        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [TestMethod]
        public void When_generating_TypeScript_classes_then_output_is_correct()
        {
            var code = Prepare(TypeScriptTypeStyle.Class);

            //// Assert
            Assert.IsTrue(code.Contains("constructor(data?: any) {"));
        }

        [TestMethod]
        public void When_generating_TypeScript_knockout_classes_then_output_is_correct()
        {
            var code = Prepare(TypeScriptTypeStyle.KnockoutClass);

            //// Assert
            Assert.IsTrue(code.Contains("name = ko.observable<string>();"));
        }

        private static string Prepare(TypeScriptTypeStyle style)
        {
            var schema = JsonSchema4.FromType<MyClassTest>();
            var data = schema.ToJson();
            var settings = new TypeScriptGeneratorSettings
            {
                TypeStyle = style
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, settings);
            var code = generator.GenerateFile();
            return code;
        }
    }
}