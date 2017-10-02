using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;

namespace NJsonSchema
{
    public interface IJsonSchemaVisitor
    {
        Task VisitAsync(object item);

        Task VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint);
    }

    public abstract class JsonSchemaVisitor : IJsonSchemaVisitor
    {
        public async Task VisitAsync(object item)
        {
            await VisitAsync(item, "#", null, new HashSet<object>());
        }

        protected virtual async Task VisitAsync(object obj, string path, string typeNameHint, HashSet<object> checkedObjects)
        {
            if (obj is JsonSchema4)
            {
                await VisitSchemaAsync((JsonSchema4)obj, path, typeNameHint, checkedObjects);
            }
            else if (obj != null && !(obj is string) && !checkedObjects.Contains(obj))
            {
                // Reflection fallback

                if (obj is IDictionary)
                {
                    foreach (var key in ((IDictionary)obj).Keys)
                        await VisitAsync(((IDictionary)obj)[key], path + "/" + key, key.ToString(), checkedObjects);
                }
                else if (obj is IEnumerable)
                {
                    var i = 0;
                    foreach (var item in (IEnumerable)obj)
                    {
                        await VisitAsync(item, path + "[" + i + "]", null, checkedObjects);
                        i++;
                    }
                }
                else
                {
                    foreach (var member in ReflectionCache.GetPropertiesAndFields(obj.GetType())
                        .Where(p => p.CustomAttributes.JsonIgnoreAttribute == null))
                    {
                        var value = member.GetValue(obj);
                        if (value != null)
                            await VisitAsync(value, path + "/" + member.GetName(), member.GetName(), checkedObjects);
                    }
                }
            }
        }

        protected virtual async Task VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint, HashSet<object> checkedObjects)
        {
            if (schema == null || checkedObjects.Contains(schema))
                return;

            await VisitSchemaAsync(schema, path, typeNameHint);

            if (schema.AdditionalItemsSchema != null)
                await VisitSchemaAsync(schema.AdditionalItemsSchema, path + "/additionalProperties", null, checkedObjects);

            if (schema.AdditionalPropertiesSchema != null)
                await VisitSchemaAsync(schema.AdditionalItemsSchema, path + "/additionalProperties", null, checkedObjects);

            if (schema.Item != null)
                await VisitSchemaAsync(schema.Item, path + "/items", null, checkedObjects);

            for (var i = 0; i < schema.Items.Count; i++)
                await VisitSchemaAsync(schema.Items.ElementAt(i), path + "/items[" + i + "]", null, checkedObjects);

            for (var i = 0; i < schema.AllOf.Count; i++)
                await VisitSchemaAsync(schema.AllOf.ElementAt(i), path + "/allOf[" + i + "]", null, checkedObjects);

            for (var i = 0; i < schema.AnyOf.Count; i++)
                await VisitSchemaAsync(schema.AnyOf.ElementAt(i), path + "/anyOf[" + i + "]", null, checkedObjects);

            for (var i = 0; i < schema.OneOf.Count; i++)
                await VisitSchemaAsync(schema.OneOf.ElementAt(i), path + "/oneOf[" + i + "]", null, checkedObjects);

            if (schema.Not != null)
                await VisitSchemaAsync(schema.Not, path + "/not", null, checkedObjects);

            foreach (var p in schema.Properties)
                await VisitSchemaAsync(p.Value, path + "/properties/" + p.Key, p.Key, checkedObjects);

            foreach (var p in schema.PatternProperties)
                await VisitSchemaAsync(p.Value, path + "/patternProperties/" + p.Key, null, checkedObjects);

            foreach (var p in schema.Definitions)
                await VisitSchemaAsync(p.Value, path + "/definitions/" + p.Key, p.Key, checkedObjects);
        }

        public abstract Task VisitSchemaAsync(JsonSchema4 schema, string path, string typeNameHint);
    }
}
