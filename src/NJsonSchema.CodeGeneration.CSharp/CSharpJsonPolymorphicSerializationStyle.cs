namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp JSON polymorphic serialization style.</summary>
    public enum CSharpJsonPolymorphicSerializationStyle
    {
        /// <summary>Use NJsonSchema polymorphic serialization</summary>
        NJsonSchema, 

        /// <summary>Use System.Text.Json polymorphic serialization</summary>
        SystemTextJson
    }
}