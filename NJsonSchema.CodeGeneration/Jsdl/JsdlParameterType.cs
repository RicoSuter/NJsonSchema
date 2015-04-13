namespace NJsonSchema.CodeGeneration.Jsdl
{
    public enum JsdlParameterType
    {
        /// <summary>A JSON object from the POST content. </summary>
        json,

        /// <summary>A query value. </summary>
        query,

        /// <summary>An URL segment. </summary>
        segment 
    }
}