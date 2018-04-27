using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class XmlObjectTests
    {
        private const string StringArray = "StringArray";
        private const string IntArray = "IntArray";
        private const string DoubleArray = "DoubleArray";
        private const string DecimalArray = "DecimalArray";
        private const string InternalItemArray = "InternalItem";
        private const string ShouldBeThisPropertyName = "ShouldBeThisPropertyName";
        private const string Foo = "Foo";

        public class WithoutXmlAttributesDefined
        {
            public string Foo { get; set; }
            public string[] StringArray { get; set; }
            public int[] IntArray { get; set; }
            public double[] DoubleArray { get; set; }
            public decimal[] DecimalArray { get; set; }
            public WithoutXmlAttributeItem[] InternalItem { get; set; }

            public WithXmlAttribute ShouldBeThisPropertyName { get; set; }

            public class WithoutXmlAttributeItem
            {
                public string Name { get; set; }
            }

            [XmlType("ShouldNOTBeThis")]
            public class WithXmlAttribute
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
                testObject.ShouldBeThisPropertyName = new WithXmlAttribute(){Name="Test2"};
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
             * <ShouldBeThisPropertyName>
             *  <Name>Test2</Name>
             * </ShouldBeThisPropertyName>
             *</WithoutXmlAttributesDefined>
            */
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();
            
            //// Assert
            Assert.Null(schema.Xml);
            var fooProperty = schema.Properties[Foo];
            var stringArrayProperty = schema.Properties[StringArray];
            var intArrayProperty = schema.Properties[IntArray];
            var doubleArrayProperty = schema.Properties[DoubleArray];
            var decimalArrayProperty = schema.Properties[DecimalArray];
            var internalItemProperty = schema.Properties[InternalItemArray];
            var shouldBeThisNameProperty = schema.Properties[ShouldBeThisPropertyName];

            Assert.Null(internalItemProperty.Xml.Name);
            Assert.Null(stringArrayProperty.Xml.Name);
            Assert.Null(intArrayProperty.Xml.Name);
            Assert.Null(doubleArrayProperty.Xml.Name);
            Assert.Null(decimalArrayProperty.Xml.Name);
            Assert.Null(fooProperty.Xml);

            //https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.Equal(true, stringArrayProperty.Xml.Wrapped);
            Assert.Equal(true, intArrayProperty.Xml.Wrapped);
            Assert.Equal(true, doubleArrayProperty.Xml.Wrapped);
            Assert.Equal(true, decimalArrayProperty.Xml.Wrapped);
            Assert.Equal(true, internalItemProperty.Xml.Wrapped);

            Assert.Equal(typeof(string).Name, stringArrayProperty.Item.Xml.Name);
            Assert.Equal(typeof(int).Name, intArrayProperty.Item.Xml.Name);
            Assert.Equal(typeof(double).Name, doubleArrayProperty.Item.Xml.Name);
            Assert.Equal(typeof(decimal).Name, decimalArrayProperty.Item.Xml.Name);
            Assert.NotNull(internalItemProperty.Item.Xml);
            //https://github.com/swagger-api/swagger-ui/issues/2610
            Assert.NotNull(shouldBeThisNameProperty.Xml); // Make sure that type reference properties have an xml object
            Assert.Equal(shouldBeThisNameProperty.Name, shouldBeThisNameProperty.Xml.Name); // Make sure that the property name and the xml name is the same
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_without_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithoutXmlAttributesDefined>(new NJsonSchema.Generation.JsonSchemaGeneratorSettings() { GenerateXmlObjects = true });
            var schemaData = schema.ToJson();

            var schemaObject = JObject.Parse(schemaData);

            var definitionXML = schemaObject["xml"];
            Assert.Null(definitionXML);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.Null(fooPropertyXml);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.Equal(true, arrayStringPropertyOuterXml["wrapped"].Value<bool>());
            Assert.Null(arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.Equal(typeof(string).Name, arrayStringPropertyItemXml["name"]);
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

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });
            var schemaData = schema.ToJson();

            //// Assert
            Assert.Equal("NotTheSameName", schema.Xml.Name);
            Assert.Equal("http://test.shema.org/type", schema.Xml.Namespace);

            var stringArrayProperty = schema.Properties[StringArray];
            var intArrayProperty = schema.Properties["TheInts"];
            var fooProperty = schema.Properties["Foo"];
            var externalItemsProperty = schema.Properties["InternalItems"];
            var externalItemType = schema.Definitions["WithXmlAttributeItem"];
            var externalItems2Property = schema.Properties["ExternalItems2"];
            var externalItem2Type = schema.Definitions["WithXmlAttributeItem2"];
            var attributeProperty = schema.Properties["MightBeAAttribute"];
            var referenceProperty = schema.Properties["ReferenceProperty"];

            Assert.Equal("Bar", fooProperty.Xml.Name);

            Assert.Equal("TheStrings", stringArrayProperty.Xml.Name);
            Assert.Null(intArrayProperty.Xml.Name);
            //https://github.com/swagger-api/swagger-ui/issues/2601
            Assert.Equal(true, stringArrayProperty.Xml.Wrapped);

            Assert.Equal("TheString", stringArrayProperty.Item.Xml.Name);

            Assert.True(attributeProperty.Xml.Attribute);

            Assert.True(externalItemsProperty.Xml.Wrapped);
            Assert.Equal("ExternalItems", externalItemsProperty.Xml.Name);
            Assert.Equal("ExternalItem", externalItemType.Xml.Name);

            Assert.Null(externalItems2Property.Xml.Name);
            Assert.True(externalItems2Property.Xml.Wrapped);
            Assert.Equal("ExternalItem2", externalItem2Type.Xml.Name);

            //https://github.com/swagger-api/swagger-ui/issues/2610
            Assert.NotNull(referenceProperty.Xml); // Make sure that type reference properties have an xml object
            Assert.Equal(referenceProperty.Name, referenceProperty.Xml.Name); // Make sure that the property name and the xml name is the same
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_and_serialized()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });

            var schemaData = schema.ToJson();
            var schemaObject = JObject.Parse(schemaData);

            var definitionXml = schemaObject["xml"];
            Assert.Equal("NotTheSameName", definitionXml["name"]);

            var fooPropertyXml = schemaObject["properties"]["Foo"]["xml"];
            Assert.Equal("Bar", fooPropertyXml["name"]);

            var arrayStringPropertyOuterXml = schemaObject["properties"][StringArray]["xml"];
            Assert.Equal("TheStrings", arrayStringPropertyOuterXml["name"]);

            var arrayStringPropertyItemXml = schemaObject["properties"][StringArray]["items"]["xml"];
            Assert.Equal("TheString", arrayStringPropertyItemXml["name"]);

            var referencePropertyXml = schemaObject["properties"]["ReferenceProperty"]["xml"];
            Assert.Equal("ReferenceProperty", referencePropertyXml["name"]);
        }

        public class WithXmlIncorrectAttributesDefined
        {
            [XmlArray(ElementName = "TheStrings", Namespace = "http://test.shema.org/type")]
            [XmlArrayItem(ElementName = "TheString", Namespace = "http://test.shema.org/type")]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_xmlobject_generation_is_active_with_a_type_with_xml_attributes_that_are_incorrect()
        {
            var schema = await JsonSchema4.FromTypeAsync<WithXmlIncorrectAttributesDefined>(new JsonSchemaGeneratorSettings
            {
                GenerateXmlObjects = true
            });
            var schemaData = schema.ToJson();

            //// Assert
            var fooProperty = schema.Properties["Foo"];

            Assert.Null(fooProperty.Xml);
        }

        [Fact]
        public async Task When_model_objects_are_created_with_the_example_model_make_sure_that_they_are_serializable()
        {
            WithXmlAttributesDefined.CreateTestXML();
            WithoutXmlAttributesDefined.CreateTestXML();
        }
    }
}
