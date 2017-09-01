using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Annotations;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class NullPropertyTests
    {
        public class ClassRoom
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int? Size { get; set; }
        }

        [TestMethod]
        public async Task When_property_is_nullable_then_property_schema_type_is_also_null()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassRoom>();
            
            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsFalse(schema.Properties["Id"].IsRequired);
            Assert.IsFalse(schema.Properties["Name"].IsRequired);
            Assert.IsFalse(schema.Properties["Size"].IsRequired);

            Assert.IsFalse(schema.Properties["Id"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["Name"].Type.HasFlag(JsonObjectType.Null));
            Assert.IsTrue(schema.Properties["Size"].Type.HasFlag(JsonObjectType.Null));
        }

        public class NotNullAttributeClass
        {
            public string Foo { get; set; }

            [NotNull]
            public string Bar { get; set; }
        }

        [TestMethod]
        public async Task When_NotNullAttribute_is_available_then_property_is_not_nullable()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<NotNullAttributeClass>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties["Foo"].IsNullable(NullHandling.JsonSchema));
            Assert.IsFalse(schema.Properties["Bar"].IsNullable(NullHandling.JsonSchema));
        }
    }
}