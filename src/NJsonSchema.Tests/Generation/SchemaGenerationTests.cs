using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class SchemaGenerationTests
    {
        public class Foo
        {
            public Dictionary<string, string> Dictionary { get; set; }

            public Bar Bar { get; set; }

            public DateTimeOffset Time { get; set; }
        }

        public class Bar
        {
            public string Name { get; set; }
        }
        
        [TestMethod]
        public async Task When_generating_schema_with_object_property_then_additional_properties_are_not_allowed()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(false, schema.Properties["Bar"].ActualTypeSchema.AllowAdditionalProperties);
        }

        [TestMethod]
        public async Task When_generating_DateTimeOffset_property_then_format_datetime_must_be_set()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Time"].Type);
            Assert.AreEqual(JsonFormatStrings.DateTime, schema.Properties["Time"].Format);
        }

        [TestMethod]
        public async Task When_generating_schema_with_dictionary_property_then_it_must_allow_additional_properties()
        {
            //// Arrange
            
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.AreEqual(true, schema.Properties["Dictionary"].ActualSchema.AllowAdditionalProperties);
            Assert.AreEqual(JsonObjectType.String, schema.Properties["Dictionary"].ActualSchema.AdditionalPropertiesSchema.ActualSchema.Type);
            // "#/definitions/ref_7de8187d_d860_41fa_a17b_3f395c053cae"
        }

        [TestMethod]
        public async Task When_output_schema_contains_reference_then_schema_reference_path_is_human_readable()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var schemaData = schema.ToJson();

            //// Assert
            Assert.IsTrue(schemaData.Contains("#/definitions/Bar"));
        }

        public class DefaultTests
        {
            [DefaultValue(10)]
            public int Number { get; set; }
        }
        
        [TestMethod]
        public async Task When_default_value_is_set_on_property_then_default_is_set_in_schema()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DefaultTests>();

            //// Act
            var property = schema.Properties["Number"];

            //// Assert
            Assert.AreEqual(10, property.Default);
        }

        public class DictTest
        {
            public Dictionary<string, object> values { get; set; }
        }

        [TestMethod]
        public async Task When_dictionary_value_is_null_then_string_values_are_allowed()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<DictTest>();
            var schemaData = schema.ToJson();

            var data = @"{
                ""values"": { 
                    ""key"": ""value"", 
                }
            }";

            //// Act
            var errors = schema.Validate(data);

            //// Assert
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public async Task When_type_is_enumerable_it_should_not_stackoverflow_on_JSON_generation()
        {
            //// Generate JSON
            var schema = await JsonSchema4.FromTypeAsync<IEnumerable<Tuple<string, string>>>();
            var json = schema.ToJson();

            //// Should be reached and not StackOverflowed
            Assert.IsTrue(!string.IsNullOrEmpty(json));
        }

        // Used as demo for https://github.com/swagger-api/swagger-ui/issues/1056

        //public class TestClass
        //{
        //    [Required] // <= not nullable
        //    public ReferencedClass RequiredProperty { get; set; }

        //    public ReferencedClass NullableProperty { get; set; }
        //}

        //public class ReferencedClass
        //{
        //    public string Test { get; set; }
        //}

        //[TestMethod]
        //public void Demo()
        //{
        //    var schema = await JsonSchema4.FromTypeAsync<TestClass>();
        //    var json = schema.ToJson();
        //}
    }
}
