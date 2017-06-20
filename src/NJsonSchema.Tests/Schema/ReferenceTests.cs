//-----------------------------------------------------------------------
// <copyright file="ReferenceTests.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class ExtensionDataTests
    {
        [TestMethod]
        public async Task When_reference_references_schema_in_custom_properties_then_the_references_are_resolved()
        {
            //// Arrange
            var json =
@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""minItems"": 1,
  ""additionalProperties"": false,
  ""items"": {
    ""maxProperties"": 1,
    ""minProperties"": 1,
    ""additionalProperties"": false,
    ""properties"": {
      ""Ok"": {
        ""$ref"": ""#/messages/Ok""
      }
    }
  },
  ""components"": {
    ""Foo"": true,
    ""Bar"": {},
    ""Id"": {
      ""type"": ""integer"",
      ""maximum"": 4294967295.0,
      ""minimum"": 0.0
    },
    ""IdMessage"": {
      ""maxProperties"": 1,
      ""minProperties"": 1,
      ""additionalProperties"": false,
      ""required"": [
        ""Id""
      ],
      ""properties"": {
        ""Id"": {
          ""$ref"": ""#/components/Id""
        }
      }
    }
  },
  ""messages"": {
    ""Ok"": {
      ""type"": ""object"",
      ""anyOf"": [
        {
          ""$ref"": ""#/components/IdMessage""
        }
      ]
    }
  }
}";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);
            var json2 = schema.ToJson();

            //// Assert
            Assert.AreEqual(json, json2);
        }
    }
}