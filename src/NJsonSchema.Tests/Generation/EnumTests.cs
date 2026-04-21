using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

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

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyController>(new NewtonsoftJsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            // Assert
            Assert.True(json.Split(["x-enumNames"], StringSplitOptions.None).Length == 2); // enum is defined only once
            Assert.True(json.Split(["\"$ref\": \"#/definitions/MetadataSchemaType\""], StringSplitOptions.None).Length == 3); // both classes reference the enum
        }

        public class ContainerWithEnumDictionary
        {
            public Dictionary<string, MetadataSchemaType> Dictionary { get; set; }
        }

        [Fact]
        public async Task When_property_is_dictionary_with_enum_value_then_it_is_referenced()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ContainerWithEnumDictionary>(new NewtonsoftJsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Dictionary"].AdditionalPropertiesSchema.HasReference);
        }

        public enum MyEnum
        {
            Value1,
            Value2
        }

        [Fact]
        public async Task When_SerializerSettings_has_CamelCase_StringEnumConverter_then_enum_values_are_correct()
        {
            // Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    Converters =
                    {
                        new StringEnumConverter { CamelCaseText = true }
                    }
                }
            };

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyEnum>(settings);
            var json = schema.ToJson();

            // Assert
            Assert.Equal("value1", schema.Enumeration.First());
            Assert.Equal("value2", schema.Enumeration.Last());

            Assert.Equal("Value1", schema.EnumerationNames.First());
            Assert.Equal("Value2", schema.EnumerationNames.Last());
        }

        [Flags]
        public enum EnumWithFlags
        {
            Foo = 1,
            Bar = 2,
            Baz = 4,
        }

        [Fact]
        public async Task When_enum_has_FlagsAttribute_then_custom_property_is_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumWithFlags>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.IsFlagEnumerable);
            Assert.Contains("x-enumFlags", json);
        }

        public enum EnumWithoutFlags
        {
            Foo = 1,
            Bar = 2,
            Baz = 3,
        }

        [Fact]
        public async Task When_enum_does_not_have_FlagsAttribute_then_custom_property_is_not_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumWithoutFlags>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });
            var json = schema.ToJson();

            // Assert
            Assert.False(schema.IsFlagEnumerable);
            Assert.DoesNotContain("x-enumFlags", json);
        }

        public enum EnumWithDescriptions
        {
            [Description("First value description")]
            FirstValue,

            [Description("Second value description")]
            SecondValue,

            // No description for this one
            ThirdValue
        }

        [Fact]
        public async Task When_enum_has_description_attributes_then_descriptions_are_included_in_schema()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumWithDescriptions>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });
            var json = schema.ToJson();

            // Assert
            Assert.Equal(3, schema.EnumerationDescriptions.Count);
            Assert.Equal("First value description", schema.EnumerationDescriptions[0]);
            Assert.Equal("Second value description", schema.EnumerationDescriptions[1]);
            Assert.Null(schema.EnumerationDescriptions[2]); // No description for ThirdValue

            // Verify the JSON output contains the x-enumDescriptions property
            await VerifyHelper.Verify(json);
        }

        [Fact]
        public async Task When_enum_has_no_description_attributes_then_descriptions_are_not_included_in_schema()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumWithFlags>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });
            var json = schema.ToJson();

            // Assert
            Assert.Empty(schema.EnumerationDescriptions);

            // Verify the JSON output does not contain the x-enumDescriptions property
            await VerifyHelper.Verify(json);
        }


        [Fact]
        public async Task When_schema_has_x_enum_names_then_backward_compatibility_works()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;

            schema.Enumeration.Clear();
            schema.Enumeration.Add("value1");
            schema.Enumeration.Add("value2");

            schema.EnumerationNames.Clear();
            schema.EnumerationNames.Add("Name1");
            schema.EnumerationNames.Add("Name2");

            // Act
            var json = schema.ToJson();

            // Assert
            await VerifyHelper.Verify(json);
        }

        [Fact]
        public async Task When_schema_has_x_enum_varnames_then_backward_compatibility_works()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;

            schema.Enumeration.Clear();
            schema.Enumeration.Add("value1");
            schema.Enumeration.Add("value2");

            schema.EnumerationNames.Clear();
            schema.EnumerationNames.Add("VarName1");
            schema.EnumerationNames.Add("VarName2");

            // Act
            var json = schema.ToJson();

            // Assert
            await VerifyHelper.Verify(json);
        }

        [Fact]
        public async Task When_schema_has_x_enum_descriptions_then_backward_compatibility_works()
        {
            // Arrange
            var schema = new JsonSchema();
            schema.Type = JsonObjectType.String;

            schema.Enumeration.Clear();
            schema.Enumeration.Add("value1");
            schema.Enumeration.Add("value2");
            schema.Enumeration.Add("value3");

            schema.EnumerationDescriptions.Clear();
            schema.EnumerationDescriptions.Add("Desc1");
            schema.EnumerationDescriptions.Add("Desc2");
            schema.EnumerationDescriptions.Add(null);

            // Act
            var json = schema.ToJson();

            // Assert
            await VerifyHelper.Verify(json);
        }
    }
}
