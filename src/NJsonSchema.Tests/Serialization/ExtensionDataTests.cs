using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Serialization
{
    [TestClass]
    public class ExtensionDataTests
    {
        [TestMethod]
        public void When_schema_has_extension_data_property_then_property_is_in_serialized_json()
        {
            //// Arrange
            var schema = new JsonSchema4();
            schema.ExtensionData = new Dictionary<string, object>
            {
                { "Test", 123 }
            };

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.IsTrue(json.Contains(
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""Test"": 123
}"));
        }

        [TestMethod]
        public void When_json_schema_contains_unknown_data_then_extension_data_is_filled()
        {
            //// Arrange
            var json =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""Test"": 123
}";

            //// Act
            var schema = JsonSchema4.FromJson(json);

            //// Assert
            Assert.AreEqual((long)123, schema.ExtensionData["Test"]);
        }

        [TestMethod]
        public void When_no_extension_data_is_available_then_property_is_null()
        {
            //// Arrange
            var json =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
}";

            //// Act
            var schema = JsonSchema4.FromJson(json);

            //// Assert
            Assert.IsNull(schema.ExtensionData);
        }
    }
}
