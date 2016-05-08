namespace NJsonSchema.CodeGeneration
{
    /// <summary>Interface for a template.</summary>
    public interface ITemplate
    {
        /// <summary>Initializes the template with a model.</summary>
        /// <param name="model">The model.</param>
        void Initialize(object model);

        /// <summary>Renders the template.</summary>
        /// <returns>The output.</returns>
        string Render();
    }
}