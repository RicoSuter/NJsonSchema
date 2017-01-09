using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class KnownTypeGenerationTests
    {
        public class Teacher
        {
        }

        [KnownType(typeof(Teacher))]
        public class Person
        {
        }

        public class Pen
        {
        }

        public class Pencil
        {
        }

        [KnownType("GetKnownTypes")]
        public class WritingInstrument
        {
            public static Type[] GetKnownTypes()
            {
                return new[] { typeof(Pen), typeof(Pencil) };
            }
        }

        public class Container
        {
            public Person Person { get; set; }
            public WritingInstrument WritingInstrument { get; set; }
        }

        [TestMethod]
        public async Task When_KnownType_attribute_exists_then_specified_classes_are_also_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var schemaData = schema.ToJson(); 

            //// Assert
            Assert.IsTrue(schema.Definitions.Any(s => s.Key == "Teacher"));
        }

        public async Task ReproAsync()
        {
            var schema = await JsonSchema4.FromTypeAsync<Container>();
        }
        [TestMethod]
        public async Task When_KnownType_attribute_includes_method_name_then_specified_classes_are_also_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Definitions.Any(s => s.Key == "Pen"));
            Assert.IsTrue(schema.Definitions.Any(s => s.Key == "Pencil"));
        }
    }
}
