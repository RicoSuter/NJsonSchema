using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class EnumTests
    {
        public enum MetadataSchemaType
        {
            Foo,
            Bar
        }

        public class MetadataSchemaDetailViewItem
        {
            public string Id { get; set; }
            public List<MetadataSchemaType> Types { get; set; }
        }

        public class MetadataSchemaCreateRequest
        {
            public string Id { get; set; }
            public List<MetadataSchemaType> Types { get; set; }
        }

        public class MyController
        {
            public MetadataSchemaDetailViewItem MetadataSchemaDetailViewItem { get; set; }

            public MetadataSchemaCreateRequest MetadataSchemaCreateRequest { get; set; }
        }

        [Fact]
        public async Task When_enum_is_used_multiple_times_in_array_then_it_is_always_referenced()
        {
            // Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<MyController>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var json = schema.ToJson();

            // Assert
            Assert.True(json.Split(new[] { "x-enumNames" }, StringSplitOptions.None).Length == 2); // enum is defined only once
            Assert.True(json.Split(new[] { "\"$ref\": \"#/definitions/MetadataSchemaType\"" }, StringSplitOptions.None).Length == 3); // both classes reference the enum
        }

        public class ContainerWithEnumDictionary
        {
            public Dictionary<string, MetadataSchemaType> Dictionary { get; set; }
        }

        [Fact]
        public async Task When_property_is_dictionary_with_enum_value_then_it_is_referenced()
        {
            // Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ContainerWithEnumDictionary>(new JsonSchemaGeneratorSettings
            {
                DefaultEnumHandling = EnumHandling.Integer
            });
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Dictionary"].AdditionalPropertiesSchema.HasReference); 
        }
    }
}