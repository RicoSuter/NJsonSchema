using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AttributeGenerationTests
    {
        [Fact]
        public async Task When_minLength_and_maxLength_attribute_are_set_on_array_then_minItems_and_maxItems_are_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Items"];

            // Assert
            Assert.Equal(3, property.MinItems);
            Assert.Equal(5, property.MaxItems);
        }

        [Fact]
        public async Task When_minLength_and_maxLength_attribute_are_set_on_string_then_minLength_and_maxLenght_are_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["String"];

            // Assert
            Assert.Equal(3, property.MinLength);
            Assert.Equal(5, property.MaxLength);
        }

        [Fact]
        public async Task When_Range_attribute_is_set_on_double_then_minimum_and_maximum_are_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Double"];

            // Assert
            Assert.Equal(5.5m, property.Minimum);
            Assert.Equal(10.5m, property.Maximum);
        }

        [Fact]
        public async Task When_Range_attribute_has_double_max_then_max_is_not_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["DoubleOnlyMin"];

            // Assert
            Assert.Equal(5.5m, property.Minimum);
            Assert.Null(property.Maximum);
        }

        [Fact]
        public async Task When_Range_attribute_is_set_on_integer_then_minimum_and_maximum_are_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Integer"];

            // Assert
            Assert.Equal(5, property.Minimum);
            Assert.Equal(10, property.Maximum);
        }

        [Fact]
        public async Task When_display_attribute_is_available_then_name_and_description_are_read()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Display"];

            // Assert
            Assert.Equal("Foo", property.Title);
            Assert.Equal("Bar", property.Description);
        }

        [Fact]
        public async Task When_display_attribute_with_resource_type_is_available_then_name_and_description_are_read() 
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["DisplayWithResource"];

            // Assert
            Assert.Equal(AttributeGenerationTestsResources.AttributeGenerationsTests_Name, property.Title);
            Assert.Equal(AttributeGenerationTestsResources.AttributeGenerationsTests_Description, property.Description);
        }

        [Fact]
        public async Task When_description_attribute_is_available_then_description_are_read()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Description"];

            // Assert
            Assert.Equal("Abc", property.Description);
        }

        [Fact]
        public async Task When_required_attribute_is_available_then_property_is_required()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Required"];

            // Assert
            Assert.True(property.IsRequired);
        }

        [Fact]
        public async Task When_required_attribute_is_not_available_then_property_is_can_be_null()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["Description"];

            // Assert
            Assert.False(property.IsRequired);
            Assert.True(property.Type.HasFlag(JsonObjectType.Null));
        }

        [Fact]
        public async Task When_ReadOnly_is_set_then_readOnly_is_set_in_schema()
        {
            // Arrange


            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AttributeTestClass>();
            var property = schema.Properties["ReadOnly"];

            // Assert
            Assert.True(property.IsReadOnly);
        }

        public class AttributeTestClass
        {
            [MinLength(3)]
            [MaxLength(5)]
            public string[] Items { get; set; }

            [MinLength(3)]
            [MaxLength(5)]
            public string String { get; set; }

            [Range(5.5, 10.5)]
            public double Double { get; set; }

            [Range(5.5, double.MaxValue)]
            public double DoubleOnlyMin { get; set; }

            [Range(5, 10)]
            public int Integer { get; set; }

            [Display(Name = "Foo", Description = "Bar")]
            public string Display { get; set; }

            [System.ComponentModel.Description("Abc")]
            public string Description { get; set; }

            [Required]
            public bool Required { get; set; }

            [ReadOnly(true)]
            public bool ReadOnly { get; set; }

            [Display(
                ResourceType = typeof(AttributeGenerationTestsResources), 
                Name = nameof(AttributeGenerationTestsResources.AttributeGenerationsTests_Name), 
                Description = nameof(AttributeGenerationTestsResources.AttributeGenerationsTests_Description)
            )]
            public string DisplayWithResource { get; set; }
        }

        public class ClassWithTypedRange
        {
            [Range(typeof(decimal), "0", "1")]
            public decimal Foo { get; set; }
        }

        [Fact]
        public async Task When_range_has_type_and_strings_then_it_is_processed_correctly()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithTypedRange>();
            var property = schema.Properties["Foo"];

            // Assert
            Assert.Equal(0.0m, property.Minimum);
            Assert.Equal(1.0m, property.Maximum);
        }

        public class ClassWithDictionary
        {
            [JsonSchemaPatternProperties(".*"), MinLength(2), MaxLength(3)]
            public Dictionary<string, string> Dict { get; set; }
        }

        [Fact]
        public void When_dictionary_property_has_attributes_then_they_are_generated_correctly()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDictionary>();
            var json = schema.ToJson();

            // Assert
            Assert.Equal(2, schema.Properties["Dict"].MinProperties);
            Assert.Equal(3, schema.Properties["Dict"].MaxProperties);
            Assert.Equal(".*", schema.Properties["Dict"].PatternProperties.First().Key);
        }

        public class ClassWithArray
        {
            [MinLength(2), MaxLength(3)]
            public List<string> Array { get; set; }
        }

        [Fact]
        public void When_array_property_has_attributes_then_they_are_generated_correctly()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithArray>();
            var json = schema.ToJson();

            // Assert
            Assert.Equal(2, schema.Properties["Array"].MinItems);
            Assert.Equal(3, schema.Properties["Array"].MaxItems);
        }

        public class ClassWithRegexDictionaryProperty
        {
            [RegularExpression("^\\d+\\.\\d+\\.\\d+\\.\\d+$")]
            public Dictionary<string, string> Versions { get; set; }
        }

        [Fact]
        public async Task When_dictionary_property_has_regex_attribute_then_regex_is_added_to_additionalProperties()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithRegexDictionaryProperty>();
            var json = schema.ToJson();

            // Assert
            Assert.Null(schema.Properties["Versions"].Pattern);
            Assert.NotNull(schema.Properties["Versions"].AdditionalPropertiesSchema.ActualSchema.Pattern);
        }
    }
}