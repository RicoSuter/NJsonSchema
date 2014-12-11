namespace NJsonSchema.DraftV4
{
    /// <summary>A JSON schema describing a type. </summary>
    public class JsonSchema : JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchema"/> class. </summary>
        public JsonSchema()
        {
            Schema = "http://json-schema.org/draft-04/schema#";
        }

        /// <summary>Creates a <see cref="JsonSchema"/> from a given type. </summary>
        /// <typeparam name="TType">The type to create the schema for. </typeparam>
        /// <returns>The <see cref="JsonSchema"/>. </returns>
        public static JsonSchema FromType<TType>()
        {
            var generator = new JsonSchemaGenerator();
            return generator.Generate<JsonSchema>(typeof(TType));
        }
    }
}
