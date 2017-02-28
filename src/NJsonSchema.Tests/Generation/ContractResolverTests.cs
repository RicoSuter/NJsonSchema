using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class ContractResolverTests
    {
        [TestMethod]
        public async Task Properties_should_match_custom_resolver()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>(new JsonSchemaGeneratorSettings
            {
                ContractResolver = new OnlyWritableCamelCaseContractResolver()
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("firstName"));
            Assert.AreEqual("firstName", schema.Properties["firstName"].Name);

            Assert.IsFalse(schema.Properties.ContainsKey("nameLength"));
        }

        public class OnlyWritableCamelCaseContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (memberSerialization == MemberSerialization.OptOut)
                {
                    var jAttribute = member.GetCustomAttribute<JsonPropertyAttribute>(true);
                    if (!prop.Writable && jAttribute == null)
                        prop.ShouldSerialize = o => false;
                }

                return prop;
            }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public int NameLength => FirstName.Length;
        }
    }
}
