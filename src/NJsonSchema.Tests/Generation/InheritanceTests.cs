using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

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
            Assert.AreEqual("Teacher", schema.TypeNameRaw);
            Assert.IsNotNull(schema.Properties["Class"]);

            Assert.AreEqual(1, schema.AllOf.Count);
            Assert.AreEqual("Person", schema.AllOf.First().ActualSchema.TypeNameRaw);
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

        [TestMethod]
        public void When_generating_type_with_inheritance_and_flattening_then_schema_has_all_properties_of_inherited_classes()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<CC>(new JsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true
            });
            var data = schema.ToJson();

            //// Assert
            Assert.AreEqual(3, schema.Properties.Count);
        }

        public class AA
        {
            public string FirstName { get; set; }
        }

        public class BB : AA
        {
            public string LastName { get; set; }
        }

        public class CC : BB
        {
            public string Address { get; set; }
        }

    }
}
