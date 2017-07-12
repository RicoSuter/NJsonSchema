using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    /// <summary> Perform calculations related to attributes </summary>
    public static class AttributeUtilities
    {
        /// <summary>
        /// Check whether a property should be ignored in generation
        /// </summary>
        /// <param name="propertyCustomAttributes">The custom attributes of the property</param>
        /// <param name="ignoredAttributes">Attributes to ignore</param>
        /// <returns></returns>
        public static bool PropertyIsIgnored(ReflectionCache.CustomAttributes propertyCustomAttributes, IgnoredPropertyAttributes ignoredAttributes)
        {
            var result = false;

            if (propertyCustomAttributes.JsonIgnoreAttribute != null)
                result = true;
            else if (propertyCustomAttributes.ObsoleteAttribute != null && ignoredAttributes.ContainsType(propertyCustomAttributes.ObsoleteAttribute))
                result = true;

            return result;
        }
    }
}
