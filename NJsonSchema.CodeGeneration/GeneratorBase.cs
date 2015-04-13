using System.IO;
using Antlr4.StringTemplate;

namespace NJsonSchema.CodeGeneration
{
    public class GeneratorBase
    {
        protected Template LoadTemplate(string language, string file)
        {
            var resourceName = string.Format("NJsonSchema.CodeGeneration.Templates.{0}.{1}.txt", language, file);
            using (var stream = GetType().Assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();
                    return new Template(text);
                }
            }
        }
    }
}