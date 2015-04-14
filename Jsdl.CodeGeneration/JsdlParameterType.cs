namespace Jsdl.CodeGeneration
{
    public enum JsdlParameterType
    {
        /// <summary>A JSON object as POST or PUT content (only one parameter of this type is allowed). </summary>
        json,

        /// <summary>A query value. </summary>
        query,

        /// <summary>An URL segment. </summary>
        segment 
    }
}