using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class ExcplicitInterfacePropertyTests
    {
        public interface IFoo
        {
            string Prop { get; set; }
        }

        public class Foo : IFoo
        {
            string IFoo.Prop { get; set; }

            public string MyProp { get; set; }
        }

        [TestMethod]
        public void When_a_property_is_explicit_interface_then_it_is_not_serialized()
        {
            //// Arrange
            var foo = new Foo();
            ((IFoo)foo).Prop = "foobar";

            //// Act
            var json = JsonConvert.SerializeObject(foo);

            //// Assert
            Assert.AreEqual(@"{""MyProp"":null}", json);
        }

        [TestMethod]
        public async Task When_a_property_is_explicit_interface_then_it_is_not_added_to_schema()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.AreEqual(1, schema.Properties.Count);
        }
    }
}
