using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class XmlObjectTests
    {
        private const string StringArray = "StringArray";
        private const string IntArray = "IntArray";
        private const string DoubleArray = "DoubleArray";
        private const string DecimalArray = "DecimalArray";
        private const string InternalItemArray = "InternalItem";
        private const string Foo = "Foo";

        public class WithoutXmlAttributesDefined
        {
            public string Foo { get; set; }
            public string[] StringArray { get; set; }
            public int[] IntArray { get; set; }
            public double[] DoubleArray { get; set; }
            public decimal[] DecimalArray { get; set; }
            public WithoutXmlAttributeItem[] InternalItem { get; set; }

            public class WithoutXmlAttributeItem
            {
                public string Name { get; set; }
            }

            public static string CreateTestXML()
            {
                var types = new System.Collections.Generic.List<System.Type>();
                types.Add(typeof(WithoutXmlAttributesDefined));
                var serializer = XmlSerializer.FromTypes(types.ToArray()).First();
                var testObject = new WithoutXmlAttributesDefined();
                testObject.Foo = "stringvalue";
                testObject.StringArray = new string[] { "S1" };
                testObject.IntArray = new int[] { 1 };
                testObject.DoubleArray = new double[] { 1 };
                testObject.DecimalArray = new decimal[] { 1 };
                testObject.InternalItem = new[] { new WithoutXmlAttributesDefined.WithoutXmlAttributeItem() { Name = "Test" } };

                var sio = new System.IO.StringWriter();
                serializer.Serialize(sio, testObject);
                return sio.ToString();
            }

            /*
             * Class above is the XML outputted as presented below by the XMLSerializer 
             *<?xml version="1.0" encoding="utf-16"?>
             *<WithoutXmlAttributesDefined xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
             * <Foo>stringvalue</Foo>
             * <StringArray>
             *   <string>S1</string>
             * </StringArray>
             * <IntArray>
             *   <int>1</int>
             * </IntArray>
             * <DoubleArray>
             *  <double>1</double>
             * </DoubleArray>
             * <DecimalArray>
             *  <decimal>1</decimal>
             * </DecimalArray>
             * <InternalItem>
             *  <WithoutXmlAttributeItem>
             *   <Name>Test</Name>
             *  </WithoutXmlAttributeItem>
             * </InternalItem>
             *</WithoutXmlAttributesDefined>
            */
        }

        [TestMethod]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();
            
            //// Assert
            Assert.IsNull(schema.Xml);
            var fooProperty = schema.Properties[Foo];
            var stringArrayProperty = schema.Properties[StringArray];
            var intArrayProperty = schema.Properties[IntArray];
            var doubleArrayProperty = schema.Properties[DoubleArray];
            var decimalArrayProperty = schema.Properties[DecimalArray];
            var internalItemProperty = schema.Properties[InternalItemArray];

            Assert.IsNull(internalItemProperty.Xml.Name);
            Assert.IsNull(stringArrayProperty.Xml.Name);
            Assert.IsNull(intArrayProperty.Xml.Name);
            Assert.IsNull(doubleArrayProperty.Xml.Name);
            Assert.IsNull(decimalArrayProperty.Xml.Name);
            Assert.IsNull(fooProperty.Xml);

            //https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.AreEqual(true, stringArrayProperty.Xml.Wrapped);
            Assert.AreEqual(true, intArrayProperty.Xml.Wrapped);
            Assert.AreEqual(true, doubleArrayProperty.Xml.Wrapped);
            Assert.AreEqual(true, decimalArrayProperty.Xml.Wrapped);
            Assert.AreEqual(true, internalItemProperty.Xml.Wrapped);

            Assert.AreEqual(typeof(string).Name, stringArrayProperty.Item.Xml.Name);
            Assert.AreEqual(typeof(int).Name, intArrayProperty.Item.Xml.Name);
            Assert.AreEqual(typeof(double).Name, doubleArrayProperty.Item.Xml.Name);
            Assert.AreEqual(typeof(decimal).Name, decimalArrayProperty.Item.Xml.Name);
            Assert.IsNull(internalItemProperty.Item.Xml);
        }

        [TestMethod]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            var schemaObject = JObject.Parse(schemaData);

            var definitionXML = schemaObject["xml"];
            Assert.IsNull(definitionXML);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.IsNull(fooPropertyXml);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.AreEqual(true, arrayStringPropertyOuterXml["wrapped"].Value<bool>());
            Assert.IsNull(arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.AreEqual(typeof(string).Name, arrayStringPropertyItemXml["name"]);
        }

        [XmlType(TypeName = "NotTheSameName", Namespace = "http://test.shema.org/type")]
        public class WithXmlAttributesDefined
        {
            [XmlElement(ElementName = "Bar", Namespace = "http://test.shema.org/type")]
            public string Foo { get; set; }

            [XmlAttribute("IsAnAttribute")]
            public string MightBeAAttribute { get; set; }

            [XmlArray(ElementName = "TheStrings", Namespace = "http://test.shema.org/type")]
            [XmlArrayItem(ElementName = "TheString", Namespace = "http://test.shema.org/type")]
            public string[] StringArray { get; set; }

            [XmlArrayItem(ElementName = "TheInt", Namespace = "http://test.shema.org/type")]
            public int[] TheInts { get; set; }

            [XmlArray("ExternalItems")]
            public WithXmlAttributeItem[] InternalItems { get; set; }

            public WithXmlAttributeItem2[] ExternalItems2 { get; set; }

            public WithXmlAttributeProperty ReferenceProperty { get; set; }

            [XmlType("ExternalItem")]
            public class WithXmlAttributeItem
            {
                public string Name { get; set; }
            }

            [XmlType("ExternalItem2")]
            public class WithXmlAttributeItem2
            {
                public string Name { get; set; }
            }

            [XmlType("NotAPropertyName")]
            public class WithXmlAttributeProperty
            {
                public string Name { get; set; }
            }

            public static string CreateTestXML()
            {
                var types = new System.Collections.Generic.List<System.Type>();
                types.Add(typeof(WithXmlAttributesDefined));
                var serializer = XmlSerializer.FromTypes(types.ToArray()).First();
                var testObject = new WithXmlAttributesDefined();
                testObject.Foo = "stringvalue";
                testObject.MightBeAAttribute = "stringvalue";
                testObject.StringArray = new string[] { "S1" };
                testObject.TheInts = new int[] { 1 };
                testObject.InternalItems = new[] { new WithXmlAttributeItem() { Name = "Test" } };
                testObject.ReferenceProperty = new WithXmlAttributeProperty() { Name = "Test" };
                testObject.ExternalItems2 = new[] { new WithXmlAttributeItem2() { Name = "Test" } };

                var sio = new System.IO.StringWriter();
                serializer.Serialize(sio, testObject);
                return sio.ToString();
            }

            /*
             * Class above is the XML outputted as presented below by the XMLSerializer 
             *<?xml version="1.0" encoding="utf-16"?>
             *<NotTheSameName xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" IsAnAttribute="stringValue2">
             * <Bar xmlns="http://test.shema.org/type">stringvalue</Bar>
             * <TheStrings xmlns="http://test.shema.org/type">
             *  <TheString>S1</TheString>
             * </TheStrings>
             * <TheInts xmlns="http://test.shema.org/type">
             *  <TheInt>1</TheInt>
             * </TheInts>
             * <ExternalItems xmlns="http://test.shema.org/type">
             *  <ExternalItem>
             *    <Name>Test</Name>
             *  </ExternalItem>
             * </ExternalItems>
             * <ExternalItems2 xmlns="http://test.shema.org/type">
             *  <ExternalItem2>
             *    <Name>Test</Name>
             *  </ExternalItem2>
             * </ExternalItems2>
             * <ReferenceProperty xmlns="http://test.shema.org/type">
             *   <Name>Test</Name>
             * </ReferenceProperty>
             *</NotTheSameName>
             * 
             */
        }

        [TestMethod]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual("NotTheSameName", schema.Xml.Name);
            Assert.AreEqual("http://test.shema.org/type", schema.Xml.Namespace);

            var stringArrayProperty = schema.Properties[StringArray];
            var intArrayProperty = schema.Properties["TheInts"];
            var fooProperty = schema.Properties["Foo"];
            var externalItemsProperty = schema.Properties["InternalItems"];
            var externalItemType = schema.Definitions["WithXmlAttributeItem"];
            var externalItems2Property = schema.Properties["ExternalItems2"];
            var externalItem2Type = schema.Definitions["WithXmlAttributeItem2"];
            var attributeProperty = schema.Properties["MightBeAAttribute"];
            var referenceProperty = schema.Properties["ReferenceProperty"];

            Assert.AreEqual("Bar", fooProperty.Xml.Name);

            Assert.AreEqual("TheStrings", stringArrayProperty.Xml.Name);
            Assert.IsNull(intArrayProperty.Xml.Name);
            //https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.AreEqual(true, stringArrayProperty.Xml.Wrapped);

            Assert.AreEqual("TheString", stringArrayProperty.Item.Xml.Name);

            Assert.IsTrue(attributeProperty.Xml.Attribute);

            Assert.IsTrue(externalItemsProperty.Xml.Wrapped);
            Assert.AreEqual("ExternalItems", externalItemsProperty.Xml.Name);
            Assert.AreEqual("ExternalItem", externalItemType.Xml.Name);

            Assert.IsNull(externalItems2Property.Xml.Name);
            Assert.IsTrue(externalItems2Property.Xml.Wrapped);
            Assert.AreEqual("ExternalItem2", externalItem2Type.Xml.Name);

            //https://github.com/swagger-api/swagger-ui/issues/2610
            Assert.IsNotNull(referenceProperty.Xml, "Make sure that type reference properties have an xml object");
            Assert.AreEqual(referenceProperty.Name, referenceProperty.Xml.Name, "Make sure that the property name and the xml name is the same");
        }

        [TestMethod]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            var schemaObject = JObject.Parse(schemaData);

            var definitionXML = schemaObject["xml"];
            Assert.AreEqual("NotTheSameName", definitionXML["name"]);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.AreEqual("Bar", fooPropertyXml["name"]);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.AreEqual("TheStrings", arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.AreEqual("TheString", arrayStringPropertyItemXml["name"]);

            var referencePropertyXml = schemaObject["properties"]["ReferenceProperty"]["xml"];
            Assert.AreEqual("ReferenceProperty", referencePropertyXml["name"]);
        }

        public class WithXmlIncorrectAttributesDefined
        {
            [XmlArray(ElementName = "TheStrings", Namespace = "http://test.shema.org/type")]
            [XmlArrayItem(ElementName = "TheString", Namespace = "http://test.shema.org/type")]
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_that_are_incorrect()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlIncorrectAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            //// Assert
            var fooProperty = schema.Properties["Foo"];

            Assert.IsNull(fooProperty.Xml);
        }

        [TestMethod]
        public async Task When_model_objects_are_created_with_the_example_model_make_sure_that_they_are_serializable()
        {
            WithXmlAttributesDefined.CreateTestXML();
            WithoutXmlAttributesDefined.CreateTestXML();
        }
    }
}
