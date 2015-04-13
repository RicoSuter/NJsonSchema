using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.Jsdl;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class JsdlTests
    {
        [TestMethod]
        public void METHOD()
        {
            //// Arrange
            var service = new JsdlService();
            service.Operations.Add("Foo", new JsdlOperation
            {
                Target = "api/Person/Delete/{0}", 
                Method = JsdlOperationMethod.delete, 
                Parameters = new List<JsdlParameter>
                {
                    new JsdlParameter
                    {
                        ParameterType = JsdlParameterType.segment,
                        SegmentPosition = 0, 
                        Type = JsonObjectType.Integer
                    }
                },
                Returns = new JsonSchema4
                {
                    Type = JsonObjectType.Object,
                    Title = "Person"
                }
            });
            service.Types.Add(JsonSchema4.FromType<Person>());

            var x = service.ToJson();
            var y = 10; 

            //// Act


            //// Assert

        }
    }
}
