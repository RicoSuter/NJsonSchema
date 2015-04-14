using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jsdl.CodeGeneration.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema;

namespace Jsdl.CodeGeneration.Tests
{
    [TestClass]
    public class JsdlTests
    {
        [TestMethod]
        public void METHOD()
        {
            //// Arrange
            var service = new JsdlService();
            service.Operations.Add(new JsdlOperation
            {
                Name = "Foo", 
                Target = "api/Person/Delete/{0}", 
                Method = JsdlOperationMethod.Delete, 
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

            var generator = new CSharpJsdlServiceGenerator(service);
            generator.Namespace = "Test";
            var code = generator.GenerateFile();
            var y = 10; 

            //// Act


            //// Assert

        }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime Birthday { get; set; }

        public Sex Sex { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }

        public string City { get; set; }
    }

    public enum Sex
    {
        Male,
        Female
    }
}
