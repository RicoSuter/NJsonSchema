using System.Globalization;
using System.IO;
using Antlr4.StringTemplate;

namespace NJsonSchema.CodeGeneration.Generators
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

        protected string ConvertToLowerStart(string name)
        {
            return name[0].ToString(CultureInfo.InvariantCulture).ToLower() + name.Substring(1);
        }

        protected string ConvertToUpperStart(string name)
        {
            return name[0].ToString(CultureInfo.InvariantCulture).ToUpper() + name.Substring(1);
        }
    }
}