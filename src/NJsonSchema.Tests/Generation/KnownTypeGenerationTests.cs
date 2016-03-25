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

        public class Container
        {
            public Person Person { get; set; }
        }

        [TestMethod]
        public void When_KnownType_attribute_exists_then_specified_classes_are_also_generated()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Container>();
            var schemaData = schema.ToJson(); 

            //// Assert
            Assert.IsTrue(schema.Definitions.Any(s => s.Value.TypeName == "Teacher"));
        }
    }
}
