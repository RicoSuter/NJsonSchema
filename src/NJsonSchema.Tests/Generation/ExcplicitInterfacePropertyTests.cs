using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
        public void When_a_property_is_explicit_interface_then_it_is_not_serialized()
        {
            //// Arrange
            var foo = new Foo();
            ((IFoo)foo).Prop = "foobar";

            //// Act
            var json = JsonConvert.SerializeObject(foo);

            //// Assert
            Assert.Equal(@"{""MyProp"":null}", json);
        }

        [Fact]
        public async Task When_a_property_is_explicit_interface_then_it_is_not_added_to_schema()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(1, schema.Properties.Count);
        }
    }
}
