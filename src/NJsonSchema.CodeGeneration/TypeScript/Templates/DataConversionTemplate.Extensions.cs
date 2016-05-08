namespace NJsonSchema.CodeGeneration.TypeScript.Templates
{
    public partial class DataConversionTemplate
    {
        internal DataConversionTemplate(dynamic model)
        {
            Model = model;
        }

        internal dynamic Model { get; set; }
    }
}
