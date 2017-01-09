using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NJsonSchema.Infrastructure
{
    /// <summary>
    /// Extension methods to help out generating XMLObject structure to schema
    /// </summary>
    public static class XmlObjectExtension
    {
        /// <summary>
        /// Generate XML object for a schema definition
        /// </summary>
        /// <param name="schema">The definition</param>
        /// <param name="type">The type of the definition</param>
        public static void GenerateXmlObjectForType(this JsonSchema4 schema, Type type)
        {
            var attributes = type.GetTypeInfo().GetCustomAttributes();
            if (attributes.Any())
            {
                dynamic xmlTypeAttribute = attributes.TryGetIfAssignableTo("System.Xml.Serialization.XmlTypeAttribute");
                if (xmlTypeAttribute != null)
                {
                    GenerateXmlObject(xmlTypeAttribute.TypeName, xmlTypeAttribute.Namespace, false, false, schema);
                }
            }
        }
        /// <summary>
        /// Generates XMLObject structure for an array with primitive types
        /// </summary>
        /// <param name="schema">The schema for the item</param>
        /// <param name="type"></param>
        public static void GenerateXmlObjectForItemType(this JsonSchema4 schema, Type type)
        {
            //Is done all the time for XML to be able to get type name as the element name
            GenerateXmlObject(type.Name, null, false, false, schema);
        }

        /// <summary>
        /// Generates XMLObject structure for a property
        /// </summary>
        /// <param name="propertySchema">The schema for the property</param>
        /// <param name="type">The type</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="attributes">Attributes that exists for the property</param>
        public static void GenerateXmlObjectForProperty(this JsonProperty propertySchema, Type type, string propertyName, IEnumerable<Attribute> attributes)
        {
            string xmlName = null;
            string xmlNamespace = null;
            bool xmlWrapped = false;

            if (propertySchema.Type == JsonObjectType.Array)
            {
                dynamic xmlArrayAttribute = attributes.TryGetIfAssignableTo("System.Xml.Serialization.XmlArrayAttribute");
                if (xmlArrayAttribute != null)
                {
                    xmlName = xmlArrayAttribute.ElementName;
                    xmlNamespace = xmlArrayAttribute.Namespace;
                }

                dynamic xmlArrayItemsAttribute = attributes.TryGetIfAssignableTo("System.Xml.Serialization.XmlArrayItemAttribute");
                if (xmlArrayItemsAttribute != null)
                {
                    var xmlItemName = xmlArrayItemsAttribute.ElementName;
                    var xmlItemNamespace = xmlArrayItemsAttribute.Namespace;

                    GenerateXmlObject(xmlItemName, xmlItemNamespace, true, false, propertySchema.Item);
                }

                xmlWrapped = true;
            }

            dynamic xmlElementAttribute = attributes.TryGetIfAssignableTo("System.Xml.Serialization.XmlElementAttribute");
            if (xmlElementAttribute != null)
            {
                xmlName = xmlElementAttribute.ElementName;
                xmlNamespace = xmlElementAttribute.Namespace;
            }

            dynamic xmlAttribute = attributes.TryGetIfAssignableTo("System.Xml.Serialization.XmlAttributeAttribute");
            if (xmlAttribute != null)
            {
                if (!String.IsNullOrEmpty(xmlAttribute.AttributeName))
                    xmlName = xmlAttribute.AttributeName;
                if (!String.IsNullOrEmpty(xmlAttribute.Namespace))
                    xmlNamespace = xmlAttribute.Namespace;
            }

            if (!String.IsNullOrEmpty(xmlName) || xmlWrapped)
                GenerateXmlObject(xmlName, xmlNamespace, xmlWrapped, xmlAttribute != null ? true : false, propertySchema);
        }

        private static void GenerateXmlObject(string name, string @namespace, bool wrapped, bool isAttribute, JsonSchema4 schema)
        {
            schema.Xml = new JsonXmlObject()
            {
                Name = name,
                Wrapped = wrapped,
                Namespace = @namespace,
                ParentSchema = schema,
                Attribute = isAttribute
            };
        }

    }
}
