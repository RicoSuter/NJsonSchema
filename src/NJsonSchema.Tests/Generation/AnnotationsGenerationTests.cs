using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class AnnotationsGenerationTests
    {
        public class AnnotationClass
        {
            public MyPoint Point { get; set; }

            [JsonSchema(JsonObjectType.String, Format = "point")]
            public AnnotationClass ClassAsString { get; set; }

            [JsonSchema(JsonObjectType.String, Format = "point")]
            public class MyPoint
            {
                public decimal X { get; set; }

                public decimal Y { get; set; }
            }
        }

        [Fact]
        public async Task When_class_annotation_is_available_then_type_and_format_can_be_customized()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AnnotationClass>();
            var data = schema.ToJson();

            // Act
            var property = schema.Properties["Point"];

            // Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("point", property.Format);
        }

        [Fact]
        public async Task When_property_annotation_is_available_then_type_and_format_can_be_customized()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<AnnotationClass>();
            var data = schema.ToJson();

            // Act
            var property = schema.Properties["ClassAsString"];

            // Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("point", property.Format);
        }

        public class DateAttributeClass
        {
            [JsonSchemaDate]
            public DateTime Date { get; set; }
        }

        [Fact]
        public async Task When_DateTime_property_has_JsonSchemaDate_attribute_then_format_and_type_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DateAttributeClass>();
            var data = schema.ToJson();

            // Act
            var property = schema.Properties["Date"];

            // Assert
            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.Equal("date", property.Format);
        }

        public class MultipleOfClass
        {
            [MultipleOf(4.5)]
            public double Number { get; set; }
        }

        [Fact]
        public async Task When_multipleOf_attribute_is_available_then_value_is_set_in_schema()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MultipleOfClass>();
            var property = schema.Properties["Number"];

            // Assert
            Assert.Equal(4.5m, property.MultipleOf.Value);
        }

        public class SimpleClass
        {
            [JsonProperty("number")]
            public decimal Number { get; set; }

            public SimpleClass(decimal number)
            {
                Number = number;
            }
        }

        [Fact]
        public async Task When_multipleOf_is_fraction_then_it_is_validated_correctly()
        {
            // Arrange
            List<SimpleClass> testClasses = [];
            for (int i = 0; i < 100; i++)
            {
                testClasses.Add(new SimpleClass((decimal)(0.1 * i)));
            }

            string jsonData = JsonConvert.SerializeObject(testClasses, Formatting.Indented);
            var schema = await JsonSchema.FromJsonAsync(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""object"",
    ""properties"": {
      ""number"": {
        ""type"": ""number"",
          ""multipleOf"": 0.1,
          ""minimum"": 0.0,
          ""maximum"": 4903700.0
      }
    },
    ""required"": [
      ""number""
    ]
  }
}");

            // Act
            var errors = schema.Validate(jsonData);

            // Assert
            Assert.Empty(errors);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel : IEnumerable<string>
        {
            public IEnumerator<string> GetEnumerator()
            {
                return null;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public async Task When_class_has_array_item_type_defined_then_schema_has_this_item_type()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ArrayModel>();

            // Act
            var data = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Item.Type);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel<T> : List<T>
        {
        }

        [Fact]
        public async Task When_class_has_array_item_type_defined_then_schema_has_this_item_type2()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ArrayModel<string>>();

            // Act
            var data = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Item.Type);
        }

        public class MyStructContainer
        {
            public MyStruct Struct { get; set; }

            public MyStruct? NullableStruct { get; set; }
        }

        [JsonSchema(JsonObjectType.String)]
        public struct MyStruct
        {
        }

        [Fact]
        public async Task When_property_is_struct_then_it_is_not_nullable()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyStructContainer>();

            // Act
            var data = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Struct"].Type);
            Assert.Equal(JsonObjectType.String | JsonObjectType.Null, schema.Properties["NullableStruct"].Type);
        }

        public class StringLengthAttributeClass
        {
            [StringLength(10, MinimumLength = 5)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_StringLengthAttribute_is_set_then_minLength_and_maxLenght_is_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<StringLengthAttributeClass>();

            // Assert
            var property = schema.Properties["Foo"];

            Assert.Equal(5, property.MinLength);
            Assert.Equal(10, property.MaxLength);
        }

        public class MinLengthAttributeClass
        {
            [MinLength(1)]
            public int[] Items { get; set; }

            [MinLength(50)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_MinLengthAttribute_is_set_then_minItems_or_minLength_is_set()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MinLengthAttributeClass>();

            var arrayProperty = schema.Properties["Items"];
            Assert.Equal(1, arrayProperty.MinItems);

            var stringProperty = schema.Properties["Foo"];
            Assert.Equal(50, stringProperty.MinLength);
        }

        public class MaxLengthAttributeClass
        {
            [MaxLength(100)]
            public int[] Items { get; set; }

            [MaxLength(500)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_MaxLengthAttribute_is_set_then_maxItems_or_maxLength_is_set()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MaxLengthAttributeClass>();

            var arrayProperty = schema.Properties["Items"];
            Assert.Equal(100, arrayProperty.MaxItems);

            var stringProperty = schema.Properties["Foo"];
            Assert.Equal(500, stringProperty.MaxLength);
        }

        public class StringRequiredClass
        {
            [Required(AllowEmptyStrings = false)]
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_RequiredAttribute_is_set_with_AllowEmptyStrings_false_then_minLength_and_required_are_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<StringRequiredClass>();

            // Assert
            var property = schema.Properties["Foo"];

            Assert.Equal(1, property.MinLength);
            Assert.True(property.IsRequired);
        }

        public class DtoRequiredClass
        {
            [Required(AllowEmptyStrings = false)]
            public StringRequiredClass Foo { get; set; }
        }

        [Fact]
        public async Task When_RequiredAttribute_is_set_with_AllowEmptyStrings_false_on_class_property_then_minLength_is_not_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DtoRequiredClass>();
            var json = schema.ToJson();

            // Assert
            var property = schema.Properties["Foo"];

            Assert.Null(property.MinLength);
            Assert.True(property.IsRequired);
        }

        public class DataTypeAttributeClass
        {
            [DataType(DataType.EmailAddress)]
            public string EmailAddress { get; set; }

            [DataType(DataType.PhoneNumber)]
            public string PhoneNumber { get; set; }

            [DataType(DataType.DateTime)]
            public string DateTime { get; set; }

            [DataType(DataType.Date)]
            public string Date { get; set; }

            [DataType(DataType.Time)]
            public string Time { get; set; }

            [DataType(DataType.Url)]
            public string Url { get; set; }

            [EmailAddress] // should be equivalent to [DataType(DataType.EmailAddress)]
            public string EmailAddress2 { get; set; }

            [Phone] // should be equivalent to [DataType(DataType.PhoneNumber)]
            public string PhoneNumber2 { get; set; }

            [Url] // should be equivalent to [DataType(DataType.Url)]
            public string Url2 { get; set; }
        }

        [Theory]
        [InlineData(nameof(DataTypeAttributeClass.EmailAddress), "email")]
        [InlineData(nameof(DataTypeAttributeClass.PhoneNumber), "phone")]
        [InlineData(nameof(DataTypeAttributeClass.DateTime), "date-time")]
        [InlineData(nameof(DataTypeAttributeClass.Time), "time")]
        [InlineData(nameof(DataTypeAttributeClass.Url), "uri")]
        [InlineData(nameof(DataTypeAttributeClass.EmailAddress2), "email")]
        [InlineData(nameof(DataTypeAttributeClass.PhoneNumber2), "phone")]
        [InlineData(nameof(DataTypeAttributeClass.Url2), "uri")]
        public async Task When_DataTypeAttribute_is_set_then_the_format_property_should_come_from_the_attribute(string propertyName, string expectedFormat)
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DataTypeAttributeClass>();
            var property = schema.Properties[propertyName];

            Assert.Equal(expectedFormat, property.Format);
        }

        [JsonSchemaIgnore]
        public class BaseObject
        {
            public string Foo { get; set; }
        }

        public class Person : BaseObject
        {
            public string Bar { get; set; }
        }

        public class Student : Person
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_class_is_ignored_then_it_is_not_in_definitions()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Student>();
            var json = schema.ToJson();

            // Assert
            Assert.False(schema.Definitions.ContainsKey("BaseObject"));
        }
    }
}