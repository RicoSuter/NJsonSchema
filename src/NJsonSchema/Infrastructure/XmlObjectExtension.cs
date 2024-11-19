//-----------------------------------------------------------------------
// <copyright file="XmlObjectExtension.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using System;
using System.Linq;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Extension methods to help out generating XMLObject structure to schema.</summary>
    public static class XmlObjectExtension
    {
        /// <summary>Generate XML object for a JSON Schema definition.</summary>
        /// <param name="schema">The JSON Schema.</param>
        /// <param name="type">The type of the JSON Schema.</param>
        public static void GenerateXmlObjectForType(this JsonSchema schema, Type type)
        {
            var attributes = type.ToCachedType().GetAttributes(true);
            if (attributes.Any())
            {
                dynamic? xmlTypeAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlTypeAttribute");
                if (xmlTypeAttribute != null)
                {
                    GenerateXmlObject(xmlTypeAttribute.TypeName, xmlTypeAttribute.Namespace, false, false, schema);
                }
            }
        }

        /// <summary>Generates an XML object for a JSON Schema definition.</summary>
        /// <param name="schema">The JSON Schema</param>
        public static void GenerateXmlObjectForArrayType(this JsonSchema schema)
        {
            if (schema.IsArray && schema.ParentSchema == null)
            {
                GenerateXmlObject($@"ArrayOf{schema.Item?.Xml?.Name}", null, true, false, schema);
            }
        }

        /// <summary>Generates XMLObject structure for an array with primitive types</summary>
        /// <param name="schema">The JSON Schema of the item.</param>
        /// <param name="type">The item type.</param>
        public static void GenerateXmlObjectForItemType(this JsonSchema schema, CachedType type)
        {
            // Is done all the time for XML to be able to get type name as the element name if not there was an attribute defined since earlier
            var attributes = type.GetAttributes(true);
            dynamic? xmlTypeAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlTypeAttribute");

            var itemName = GetXmlItemName(type.OriginalType);
            if (xmlTypeAttribute != null)
            {
                itemName = xmlTypeAttribute.TypeName;
            }

            GenerateXmlObject(itemName, null, false, false, schema);
        }

        /// <summary>Generates XMLObject structure for a property.</summary>
        /// <param name="propertySchema">The JSON Schema for the property</param>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">The property name.</param>
        public static void GenerateXmlObjectForProperty(this JsonSchemaProperty propertySchema, ContextualType type, string propertyName)
        {
            string? xmlName = null;
            string? xmlNamespace = null;
            bool xmlWrapped = false;

            if (propertySchema.IsArray)
            {
                dynamic? xmlArrayAttribute = type.GetContextOrTypeAttributes<Attribute>(true).FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlArrayAttribute");
                if (xmlArrayAttribute != null)
                {
                    xmlName = xmlArrayAttribute.ElementName;
                    xmlNamespace = xmlArrayAttribute.Namespace;
                }

                dynamic? xmlArrayItemsAttribute = type.GetContextOrTypeAttributes<Attribute>(true).FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlArrayItemAttribute");
                if (xmlArrayItemsAttribute != null)
                {
                    var xmlItemName = xmlArrayItemsAttribute.ElementName;
                    var xmlItemNamespace = xmlArrayItemsAttribute.Namespace;

                    GenerateXmlObject(xmlItemName, xmlItemNamespace, true, false, propertySchema.Item);
                }

                xmlWrapped = true;
            }

            dynamic? xmlElementAttribute = type.GetContextOrTypeAttributes<Attribute>(true).FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlElementAttribute");
            if (xmlElementAttribute != null)
            {
                xmlName = xmlElementAttribute.ElementName;
                xmlNamespace = xmlElementAttribute.Namespace;
            }

            dynamic? xmlAttribute = type.GetContextOrTypeAttributes<Attribute>(true).FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlAttributeAttribute");
            if (xmlAttribute != null)
            {
                if (!string.IsNullOrEmpty(xmlAttribute.AttributeName))
                {
                    xmlName = xmlAttribute.AttributeName;
                }

                if (!string.IsNullOrEmpty(xmlAttribute.Namespace))
                {
                    xmlNamespace = xmlAttribute.Namespace;
                }
            }

            // Due to that the JSON Reference is used, the xml name from the referenced type will be copied to the property.
            // We need to ensure that the property name is preserved
            if (string.IsNullOrEmpty(xmlName) && propertySchema.Type == JsonObjectType.None)
            {
                dynamic? xmlReferenceTypeAttribute = type.GetAttributes(true).FirstAssignableToTypeNameOrDefault("System.Xml.Serialization.XmlTypeAttribute");
                if (xmlReferenceTypeAttribute != null)
                {
                    xmlName = propertyName;
                }
            }

            if (!string.IsNullOrEmpty(xmlName) || xmlWrapped)
            {
                GenerateXmlObject(xmlName, xmlNamespace, xmlWrapped, xmlAttribute != null ? true : false, propertySchema);
            }
        }

        private static void GenerateXmlObject(string? name, string? @namespace, bool wrapped, bool isAttribute, JsonSchema schema)
        {
            schema.Xml = new JsonXmlObject
            {
                Name = name,
                Wrapped = wrapped,
                Namespace = @namespace,
                ParentSchema = schema,
                Attribute = isAttribute
            };
        }

        /// <summary>type.Name is used int will return Int32, string will return String etc. 
        /// These are not valid with how the XMLSerializer performs.</summary>
        private static string GetXmlItemName(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(double))
            {
                return "double";
            }
            else if (type == typeof(decimal))
            {
                return "decimal";
            }
            else
            {
                return type.Name;
            }
        }
    }
}
