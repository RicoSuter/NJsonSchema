using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NJsonSchema.CodeGeneration.Generators;

namespace Jsdl.CodeGeneration.Generators
{
    public class CSharpJsdlServiceGenerator : CSharpClassGenerator
    {
        private readonly JsdlService _service;
        private readonly Dictionary<string, CSharpClassGenerator> _types;

        public CSharpJsdlServiceGenerator(JsdlService service)
            : base(null)
        {
            _service = service;
            _types = _service.Types.ToDictionary(t => t.Title, t => new CSharpClassGenerator(t));
        }

        public string Namespace { get; set; }

        public string GenerateFile()
        {
            var operations = _service.Operations.Select(operation => new
            {
                Name = operation.Name,
                MethodName = ConvertToUpperStart(operation.Name),
                
                ResultType = GetAndLoadType(operation),
                Method = operation.Method,
                
                Parameters = operation.Parameters.Select(p => new
                {
                    Name = p.Name,
                    Type = GetType(p, p.IsRequired),
                    IsLast = p == operation.Parameters.Last() 
                }),
                
                HasContent = operation.Parameters.Any(p => p.ParameterType == JsdlParameterType.json),
                ContentParameter = operation.Parameters.SingleOrDefault(p => p.ParameterType == JsdlParameterType.json),
                
                SegmentParameters = operation.Parameters.Where(p => p.ParameterType == JsdlParameterType.segment),
                QueryParameters = operation.Parameters.Where(p => p.ParameterType == JsdlParameterType.query),
                
                Target = operation.Target
            });

            var template = LoadTemplate("CSharp", "File");
            template.Add("class", _service.Name);
            template.Add("namespace", Namespace);
            template.Add("operations", operations);

            var classes = string.Empty;
            foreach (var type in _types.Values)
                classes += "\n\n" + type.GenerateClasses();

            template.Add("classes", classes);

            return template.Render();
        }

        private object GetAndLoadType(JsdlOperation operation)
        {
            if (operation.Returns.Type.HasFlag(JsonObjectType.Object))
            {
                var resultType = operation.Returns.Title;
                if (string.IsNullOrEmpty(resultType))
                    resultType = string.Format("{0}Result", operation.Name);

                if (!_types.ContainsKey(resultType))
                {
                    var generator = new CSharpClassGenerator(operation.Returns);
                    _types.Add(resultType, generator);
                }
            }

            return GetType(operation.Returns, true);
        }
    }
}
