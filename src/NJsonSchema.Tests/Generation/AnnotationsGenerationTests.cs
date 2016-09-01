using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Annotations;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
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

        [TestMethod]
        public void When_class_annotation_is_available_then_type_and_format_can_be_customized()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<AnnotationClass>();
            var schemaData = schema.ToJson();

            //// Act
            var property = schema.Properties["Point"];

            //// Assert
            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.String));
            Assert.AreEqual("point", property.Format);
        }

        [TestMethod]
        public void When_property_annotation_is_available_then_type_and_format_can_be_customized()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<AnnotationClass>();
            var schemaData = schema.ToJson();

            //// Act
            var property = schema.Properties["ClassAsString"];

            //// Assert
            Assert.IsTrue(property.Type.HasFlag(JsonObjectType.String));
            Assert.AreEqual("point", property.Format);
        }

        public class MultipleOfClass
        {
            [MultipleOf(4.5)]
            public double Number { get; set; }
        }

        [TestMethod]
        public void When_multipleOf_attribute_is_available_then_value_is_set_in_schema()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<MultipleOfClass>();
            var property = schema.Properties["Number"];

            //// Assert
            Assert.AreEqual(4.5, property.MultipleOf.Value);
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

        [TestMethod]
        public void When_class_has_array_item_type_defined_then_schema_has_this_item_type()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ArrayModel>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""string""
  }
}", json);
        }

        [JsonSchema(JsonObjectType.Array, ArrayItem = typeof(string))]
        public class ArrayModel<T> : List<T>
        {
        }

        [TestMethod]
        public void When_class_has_array_item_type_defined_then_schema_has_this_item_type2()
        {
            //// Arrange
            var schema = JsonSchema4.FromType<ArrayModel<string>>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.AreEqual(@"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": {
    ""type"": ""string""
  }
}", json);
        }

        public class StringLengthAttributeClass
        {
            [StringLength(10, MinimumLength = 5)]
            public string Foo { get; set; }
        }

        [TestMethod]
        public void When_StringLengthAttribute_is_set_then_minLength_and_maxLenght_is_set()
        {
            //// Arrange

            //// Act
            var schema = JsonSchema4.FromType<StringLengthAttributeClass>();

            //// Assert
            var property = schema.Properties["Foo"];

            Assert.AreEqual(5, property.MinLength);
            Assert.AreEqual(10, property.MaxLength);
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

            [DataType(DataType.Upload)]
            public string Upload { get; set; }
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_DateTime_then_the_format_property_is_datetime()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["DateTime"];

            Assert.AreEqual("date-time", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_Date_then_the_format_property_is_date()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["Date"];

            Assert.AreEqual("date", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_Time_then_the_format_property_is_time()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["Time"];

            Assert.AreEqual("time", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_EmailAddress_then_the_format_property_is_email()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["EmailAddress"];

            Assert.AreEqual("email", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_PhoneNumber_then_the_format_property_is_phone()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["PhoneNumber"];

            Assert.AreEqual("phone", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_Url_then_the_format_property_is_uri()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["Url"];

            Assert.AreEqual("uri", property.Format);
        }

        [TestMethod]
        public void When_DataTypeAttribute_is_Upload_then_the_format_property_is_data_url()
        {
            var schema = JsonSchema4.FromType<DataTypeAttributeClass>();
            var property = schema.Properties["Upload"];

            Assert.AreEqual("data-url", property.Format);
        }
    }
}