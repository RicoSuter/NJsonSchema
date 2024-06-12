namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The CSharp JSON polymorphic serialization style.</summary>
    public enum CSharpJsonPolymorphicSerializationStyle
    {
        /// <summary>Use NSwag polymorphic serialization</summary>
        NSwag, 

        /// <summary>Use System.Text.Json polymorphic serialization</summary>
        SystemTextJson
    }
}