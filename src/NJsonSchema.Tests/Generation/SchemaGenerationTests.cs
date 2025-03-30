﻿using NJsonSchema.NewtonsoftJson.Generation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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
        
        [Fact]
        public async Task When_generating_schema_with_object_property_then_additional_properties_are_not_allowed()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.False(schema.Properties["Bar"].ActualTypeSchema.AllowAdditionalProperties);
        }

        [Fact]
        public async Task When_generating_DateTimeOffset_property_then_format_datetime_must_be_set()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Time"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["Time"].Format);
        }

        [Fact]
        public async Task When_generating_schema_with_dictionary_property_then_it_must_allow_additional_properties()
        {
            // Arrange
            
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.True(schema.Properties["Dictionary"].ActualSchema.AllowAdditionalProperties);
            Assert.Equal(JsonObjectType.String, schema.Properties["Dictionary"].ActualSchema.AdditionalPropertiesSchema.ActualSchema.Type);
            // "#/definitions/ref_7de8187d_d860_41fa_a17b_3f395c053cae"
        }

        [Fact]
        public async Task When_output_schema_contains_reference_then_schema_reference_path_is_human_readable()
        {
            // Arrange

            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>();
            var schemaData = schema.ToJson();

            // Assert
            Assert.Contains("#/definitions/Bar", schemaData);
        }

        public class DefaultTests
        {
            [DefaultValue(10)]
            public int Number { get; set; }
        }
        
        [Fact]
        public async Task When_default_value_is_set_on_property_then_default_is_set_in_schema()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DefaultTests>();

            // Act
            var property = schema.Properties["Number"];

            // Assert
            Assert.Equal(10, property.Default);
        }

        public class DictTest
        {
            public Dictionary<string, object> values { get; set; }
        }

        [Fact]
        public async Task When_dictionary_value_is_null_then_string_values_are_allowed()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<DictTest>();
            var schemaData = schema.ToJson();

            var data = @"{
                ""values"": { 
                    ""key"": ""value"", 
                }
            }";

            // Act
            var errors = schema.Validate(data);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task When_type_is_enumerable_it_should_not_stackoverflow_on_JSON_generation()
        {
            // Generate JSON
            var schema = NewtonsoftJsonSchemaGenerator.FromType<IEnumerable<Tuple<string, string>>>();
            var json = schema.ToJson();

            // Should be reached and not StackOverflowed
            Assert.True(!string.IsNullOrEmpty(json));
        }

        public class FilterDto
        {
            public long FieldId { get; set; }

            public object Value { get; set; }
        }

        [Fact]
        public async Task When_property_is_object_then_it_should_not_be_a_dictonary_but_any()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<FilterDto>();
            var json = schema.ToJson();

            // Assert
            var property = schema.Properties["Value"].ActualTypeSchema;
            Assert.True(property.IsAnyType);
            Assert.False(property.IsDictionary);
        }

        public class ClassWithStaticProperty
        {
            public static string Foo { get; set; }

            public string Bar { get; set; }
        }

        [Fact]
        public async Task When_property_is_static_then_it_is_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithStaticProperty>();
            var json = schema.ToJson();

            // Assert
            Assert.Single(schema.ActualProperties);
            Assert.True(schema.ActualProperties.ContainsKey("Bar"));
        }
        
        [DataContract]
        class ClassWithPrivateDataMember1
        {
            [IgnoreDataMember]
            public string MyField
            {
                get => _myField;
                set
                {
                    // Do some stuff, except for deserializing.
                    _myField = value;
                }
            }

            [DataMember(Name = nameof(MyField))] 
            [Display(Name = "My Field", Description = "......")]
            private string _myField;
        }

        [Fact]
        public async Task When_private_field_is_dataMember_then_it_is_not_ignored1()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithPrivateDataMember1>();
            var json = schema.ToJson();

            // Assert
            Assert.Single(schema.ActualProperties);
            Assert.True(schema.ActualProperties.ContainsKey("MyField"));
        }
        
        [DataContract]
        private class ClassWithPrivateDataMember2
        {
#pragma warning disable CS0169
            [DataMember(Name = nameof(MyField))] 
            [Display(Name = "My Field", Description = "......")]
            private int _myField;
#pragma warning restore CS0169

            [IgnoreDataMember]
            public string MyField { get; set; }
        }

        [Fact]
        public async Task When_private_field_is_dataMember_then_it_is_not_ignored2()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithPrivateDataMember2>();
            var json = schema.ToJson();

            // Assert
            Assert.Single(schema.ActualProperties);
            Assert.True(schema.ActualProperties.ContainsKey("MyField"));
            Assert.Equal(JsonObjectType.Integer, schema.Properties["MyField"].Type);
        }
        
        [DataContract]
        private class ClassWithPrivateDataMember3
        {
            [DataMember]
            private string MyField { get; set; }
        }

        [Fact]
        public async Task When_private_property_is_dataMember_then_it_is_not_ignored()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithPrivateDataMember3>();
            var json = schema.ToJson();

            // Assert
            Assert.Single(schema.ActualProperties);
            Assert.True(schema.ActualProperties.ContainsKey("MyField"));
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

        //[Fact]
        //public void Demo()
        //{
        //    var schema = await JsonSchema4.FromTypeAsync<TestClass>();
        //    var json = schema.ToJson();
        //}
    }
}
