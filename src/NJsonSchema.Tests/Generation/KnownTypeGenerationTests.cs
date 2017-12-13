using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class KnownTypeGenerationTests
    {
        public class Teacher
        {
        }

        [KnownType(typeof(Teacher))]
        public class Person
        {
        }

        public class Pen : WritingInstrument
        {
        }

        public class Pencil : WritingInstrument
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

        [Fact]
        public async Task When_KnownType_attribute_exists_then_specified_classes_are_also_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var schemaData = schema.ToJson(); 

            //// Assert
            Assert.Contains(schema.Definitions, s => s.Key == "Teacher");
        }

        public async Task ReproAsync()
        {
            var schema = await JsonSchema4.FromTypeAsync<Container>();
        }
        [Fact]
        public async Task When_KnownType_attribute_includes_method_name_then_specified_classes_are_also_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Contains(schema.Definitions, s => s.Key == "Pen");
            Assert.Contains(schema.Definitions, s => s.Key == "Pencil");
        }
    }
}
