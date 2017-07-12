using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NJsonSchema.Generation
{
    /// <summary>
    /// Ignore generating schema for properties decorated with these attributes
    /// </summary>
    public class IgnoredPropertyAttributes
    {
        /// <summary>
        /// Types of the ignored attributes
        /// </summary>
        public IList<Type> IgnoredAttributeTypes { get; set; }

        /// <summary>
        /// Make a default list containing only the JsonIgnoreAttribute
        /// </summary>
        public IgnoredPropertyAttributes()
        {
            IgnoredAttributeTypes = new List<Type>() { typeof(JsonIgnoreAttribute) };
        }

        /// <summary>
        /// Make a list of ignored attributes, adding the JsonIgnoreAttribute by default
        /// </summary>
        /// <param name="ignoreAttributeTypes"></param>
        public IgnoredPropertyAttributes(IEnumerable<Type> ignoreAttributeTypes)
        {
            IgnoredAttributeTypes = new List<Type>(ignoreAttributeTypes);

            if (!IgnoredAttributeTypes.Contains(typeof(JsonIgnoreAttribute)))
                IgnoredAttributeTypes.Add(typeof(JsonIgnoreAttribute));
        }
        
        /// <summary>
        /// Will check to see if the type of the argument attribute is in the list of ignored attributes
        /// </summary>
        public bool ContainsType(Attribute attributeType)
        {
            var type = attributeType.GetType();
            return IgnoredAttributeTypes.Contains(type);
        }
    }
}
