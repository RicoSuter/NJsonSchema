namespace NJsonSchema.DraftV4
{
    /// <summary>A JSON schema describing a type. </summary>
    public class JsonSchema4 : JsonSchemaBase
    {
        /// <summary>Initializes a new instance of the <see cref="JsonSchema4"/> class. </summary>
        public JsonSchema4()
        {
            Schema = "http://json-schema.org/draft-04/schema#";
        }

        /// <summary>Creates a <see cref="JsonSchema4"/> from a given type. </summary>
        /// <typeparam name="TType">The type to create the schema for. </typeparam>
        /// <returns>The <see cref="JsonSchema4"/>. </returns>
        public static JsonSchema4 FromType<TType>()
        {
            var generator = new JsonSchemaGenerator();
            return generator.Generate<JsonSchema4>(typeof(TType));
        }
    }
}
