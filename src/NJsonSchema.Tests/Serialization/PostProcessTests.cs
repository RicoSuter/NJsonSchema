using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NJsonSchema.Tests.Serialization
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NJsonSchema.Annotations;

    [TestClass]
    public class PostProcessTests
    {
        [JsonSchemaPostProcess(typeof(MyTest), nameof(PostProcess))]
        public class MyTest
        {
            public string Property { get; set; }

            public static void PostProcess(JsonSchema4 schema)
            {
                schema.Description = "FromPostProcess";
            }
        }

        [TestMethod]
        public async Task schema_should_be_postprocessed()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyTest>();

            //// Assert
            Assert.AreEqual("FromPostProcess", schema.Description);

        }
    }
}
