using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class EnumGenerationTests
    {
        public class Foo
        {
            public Bar Bar { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Bar Bar2 { get; set; }
        }

        /// <summary>
        /// Foo bar.
        /// </summary>
        public enum Bar
        {
            A = 0,
            B = 5,
            C = 6,
        }

        [Fact]
        public async Task When_property_is_integer_enum_then_schema_has_enum()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                GenerateEnumMappingDescription = true
            });
            var data = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["Bar"].ActualTypeSchema.Type);
            Assert.Equal(3, schema.Properties["Bar"].ActualTypeSchema.Enumeration.Count);
            Assert.Equal(0, schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(0));
            Assert.Equal(5, schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(1));
            Assert.Equal(6, schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(2));

            Assert.Contains("Foo bar.", schema.Properties["Bar"].ActualTypeSchema.Description); // option is enabled
            Assert.Contains("5 = B", schema.Properties["Bar"].ActualTypeSchema.Description); // option is enabled
        }

        [Fact]
        public async Task When_string_and_integer_enum_used_then_two_refs_are_generated()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings());
            var data = schema.ToJson();

            // Assert
            Assert.NotNull(schema.Properties["Bar"].ActualTypeSchema);
            Assert.NotNull(schema.Properties["Bar2"].ActualTypeSchema); // must not be a reference but second enum declaration
            Assert.NotEqual(schema.Properties["Bar"].ActualTypeSchema, schema.Properties["Bar2"].ActualTypeSchema);

            Assert.DoesNotContain("5 = B", schema.Properties["Bar"].ActualTypeSchema.Description); // option is not enabled
        }

        [Fact]
        public async Task When_property_is_string_enum_then_schema_has_enum()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                },
                GenerateEnumMappingDescription = true
            });
            var data = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Bar"].ActualTypeSchema.Type);
            Assert.Equal(3, schema.Properties["Bar"].ActualTypeSchema.Enumeration.Count);
            Assert.Equal("A", schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(0));
            Assert.Equal("B", schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(1));
            Assert.Equal("C", schema.Properties["Bar"].ActualTypeSchema.Enumeration.ElementAt(2));

            Assert.DoesNotContain("=", schema.Properties["Bar"].ActualTypeSchema.Description); // string enums do not have mapping in description
        }

        [Fact]
        public async Task When_enum_is_generated_then_names_are_set()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Assert
            Assert.Equal(3, schema.Properties["Bar"].ActualTypeSchema.EnumerationNames.Count);
            Assert.Equal("A", schema.Properties["Bar"].ActualTypeSchema.EnumerationNames.ElementAt(0));
            Assert.Equal("B", schema.Properties["Bar"].ActualTypeSchema.EnumerationNames.ElementAt(1));
            Assert.Equal("C", schema.Properties["Bar"].ActualTypeSchema.EnumerationNames.ElementAt(2));
        }

        public class EnumProperty
        {
            [DefaultValue(Bar.C)]
            public Bar Bar { get; set; }
        }

        [Fact]
        public async Task When_enum_property_is_generated_then_enum_is_referenced()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumProperty>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            });
            var json = schema.ToJson();

            // Assert
            Assert.Equal(Bar.C, schema.Properties["Bar"].Default);
            Assert.True(schema.Properties["Bar"].HasReference);
        }

        public class EnumPropertyWithDefaultClass
        {
            [DefaultValue(MyEnumeration.C)]
            public MyEnumeration MyEnumeration { get; set; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum MyEnumeration
        {
            A,
            B,
            C
        }

        [Fact]
        public async Task When_string_enum_property_has_default_then_default_is_converted_to_string()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<EnumPropertyWithDefaultClass>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.Equal("C", schema.Properties["MyEnumeration"].Default);
        }

        public class Party
        {
            public MyEnumeration? EnumValue { get; set; }

            public bool ShouldSerializeEnumValue()
            {
                return EnumValue.HasValue;
            }
        }

        [Fact]
        public async Task When_enum_property_has_should_serialize_then_no_npe()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Party>(new NewtonsoftJsonSchemaGeneratorSettings());

            // Act
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.Properties.ContainsKey("EnumValue"));
            Assert.NotNull(json);
        }

        public class RequiredEnumProperty
        {
            [Required]
            public Bar Bar { get; set; }

            public Bar Bar2 { get; set; }
        }

        [Fact]
        public async Task When_enum_property_is_required_then_MinLength_is_not_set()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<RequiredEnumProperty>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.OpenApi3,
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                }
            });
            var json = schema.ToJson();

            // Assert
            Assert.True(schema.RequiredProperties.Contains("Bar"));
            Assert.True(schema.Properties["Bar"].OneOf.Count == 0);
            Assert.True(schema.Properties["Bar"].Reference != null);
        }
    }
}