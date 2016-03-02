using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Conversion
{
    [TestClass]
    public class ArrayTypeToSchemaTests
    {
        [TestMethod]
        public void When_converting_type_inheriting_from_dictionary_then_it_should_be_correct()
        {
            //// Act
            var schema = JsonSchema4.FromType<DictionarySubType>();
            var data = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.Object, schema.Type);
            Assert.IsNotNull(schema.AllOf.First().Properties["Foo"]);
            Assert.AreEqual(typeof(DictionarySubType).Name, schema.TypeName);
        }

        [TestMethod]
        public void When_converting_array_then_items_must_correctly_be_loaded()
        {
            When_converting_array_then_items_must_correctly_be_loaded("Array");
        }

        [TestMethod]
        public void When_converting_collection_then_items_must_correctly_be_loaded()
        {
            When_converting_array_then_items_must_correctly_be_loaded("Collection");
        }

        [TestMethod]
        public void When_converting_list_then_items_must_correctly_be_loaded()
        {
            When_converting_array_then_items_must_correctly_be_loaded("List");
        }


        [TestMethod]
        public void When_converting_interface_list_then_items_must_correctly_be_loaded()
        {
            When_converting_array_then_items_must_correctly_be_loaded("InterfaceList");
        }

        [TestMethod]
        public void When_converting_enumerable_list_then_items_must_correctly_be_loaded()
        {
            When_converting_array_then_items_must_correctly_be_loaded("Enumerable");
        }

        public void When_converting_array_then_items_must_correctly_be_loaded(string propertyName)
        {
            //// Act
            var schema = JsonSchema4.FromType<MyType>();

            //// Assert
            var property = schema.Properties[propertyName];

            Assert.AreEqual(JsonObjectType.Array | JsonObjectType.Null, property.Type);
            Assert.AreEqual(JsonObjectType.Object | JsonObjectType.Null, property.Item.ActualSchema.Type);
            Assert.AreEqual(typeof(MySubtype).Name, property.Item.ActualSchema.TypeName);
            Assert.AreEqual(JsonObjectType.String | JsonObjectType.Null, property.Item.ActualSchema.Properties["Id"].Type);
        }

        public class DictionarySubType : DictionaryType
        {

        }

        public class DictionaryType : Dictionary<string, IList<string>>
        {
            public string Foo { get; set; }
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
    }
}
