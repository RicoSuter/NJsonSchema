using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class InheritanceTests
    {
        [TestMethod]
        public void When_generating_type_with_inheritance_then_allOf_has_one_item()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<Teacher>();

            //// Assert
            Assert.AreEqual("Teacher", schema.TypeName);
            Assert.IsNotNull(schema.Properties["Class"]);

            Assert.AreEqual(1, schema.AllOf.Count);
            Assert.AreEqual("Person", schema.AllOf.First().ActualSchema.TypeName);
            Assert.IsNotNull(schema.AllOf.First().ActualSchema.Properties["Name"]);
        }

        public class Teacher : Person
        {
            public string Class { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
        }
    }
}
