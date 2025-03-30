﻿using NJsonSchema.NewtonsoftJson.Generation;
using System.Runtime.Serialization;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class KnownTypeGenerationTests
    {
        public class SpecialTeacher : Teacher
        {
            public string Foo { get; set; }
        }

        [KnownType(typeof(SpecialTeacher))]
        public class Teacher
        {
            public string Bar { get; set; }
        }

        [KnownType(typeof(Teacher))]
        public class Person
        {
            public string Baz { get; set; }
        }

        public class Pen : WritingInstrument
        {
            public string Foo { get; set; }
        }

        public class Pencil : WritingInstrument
        {
            public string Bar { get; set; }
        }

        [KnownType("GetKnownTypes")]
        public class WritingInstrument
        {
            public static Type[] GetKnownTypes()
            {
                return [typeof(Pen), typeof(Pencil)];
            }

            public string Baz { get; set; }
        }

        public class Container
        {
            public Person Person { get; set; }

            public Teacher Teacher { get; set; }

            public WritingInstrument WritingInstrument { get; set; }
        }

        [Fact]
        public async Task When_KnownType_attribute_exists_then_specified_classes_are_also_generated()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Container>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.Contains(schema.Definitions, s => s.Key == "Teacher");
            Assert.Contains(schema.Definitions, s => s.Key == "SpecialTeacher");
        }

        [Fact]
        public async Task When_KnownType_attribute_includes_method_name_then_specified_classes_are_also_generated()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Container>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.Contains(schema.Definitions, s => s.Key == "Pen");
            Assert.Contains(schema.Definitions, s => s.Key == "Pencil");
        }
    }
}
