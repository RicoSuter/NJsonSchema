using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.Tests.Conversion
{
    public class ArrayTypeToSchemaTests
    {
        public class DictionarySubType : DictionaryType
        {

        }

        public class DictionaryType : Dictionary<string, IList<string>>
        {
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_converting_type_inheriting_from_dictionary_then_it_should_be_correct()
        {
            //// Act
            var dict = new DictionarySubType();
            dict.Foo = "abc";
            dict.Add("bar", new List<string> { "a", "b" });
            var json = JsonConvert.SerializeObject(dict);

            var schema = await JsonSchema4.FromTypeAsync<DictionarySubType>();
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.Object, schema.Type);
            Assert.DoesNotContain("Foo", json);
            Assert.DoesNotContain("foo", json);
        }

        [Fact]
        public async Task When_converting_array_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("Array");
        }

        [Fact]
        public async Task When_converting_collection_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("Collection");
        }

        [Fact]
        public async Task When_converting_list_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("List");
        }

        [Fact]
        public async Task When_converting_interface_list_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("InterfaceList");
        }

        [Fact]
        public async Task When_converting_enumerable_list_then_items_must_correctly_be_loaded()
        {
            await When_converting_smth_then_items_must_correctly_be_loaded("Enumerable");
        }

        public class MyType
        {
            public MySubtype Reference { get; set; }

            public MySubtype[] Array { get; set; }

            public Collection<MySubtype> Collection { get; set; }

            public List<MySubtype> List { get; set; }

            public IList<MySubtype> InterfaceList { get; set; }

            public IEnumerable<MySubtype> Enumerable { get; set; }
        }

        public class MySubtype
        {
            public string Id { get; set; }
        }

        private async Task When_converting_smth_then_items_must_correctly_be_loaded(string propertyName)
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyType>();
            var schemaData = schema.ToJson();

            //// Assert
            var property = schema.Properties[propertyName];

            Assert.Equal(JsonObjectType.Array | JsonObjectType.Null, property.Type);
            Assert.Equal(JsonObjectType.Object, property.ActualSchema.Item.ActualSchema.Type);
            Assert.Contains(schema.Definitions, d => d.Key == "MySubtype");
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, property.ActualSchema.Item.ActualSchema.Properties["Id"].Type);
        }
    }
}
