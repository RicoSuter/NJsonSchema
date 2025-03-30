﻿using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class DateTimeCodeGenerationTests
    {
        public class ClassWithDateTimeProperty
        {
            public DateTime MyDateTime { get; set; }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void When_date_handling_is_string_then_string_property_are_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("myDateTime: string", code);
            Assert.Contains("this.myDateTime = _data[\"MyDateTime\"];", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime;", code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void When_date_handling_is_moment_then_moment_property_are_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("myDateTime: moment.Moment", code);
            Assert.Contains("this.myDateTime = _data[\"MyDateTime\"] ? moment(_data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : <any>undefined;", code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void When_date_handling_is_offset_moment_then_moment_property_are_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.OffsetMomentJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("myDateTime: moment.Moment", code);
            Assert.Contains("this.myDateTime = _data[\"MyDateTime\"] ? moment.parseZone(_data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString(true) : <any>undefined;", code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void When_date_handling_is_dayjs_then_dayjs_property_are_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.DayJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("myDateTime: dayjs.Dayjs", code);
            Assert.Contains("this.myDateTime = _data[\"MyDateTime\"] ? dayjs(_data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : <any>undefined;", code);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void When_date_handling_is_date_then_date_property_are_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("myDateTime: Date", code);
            Assert.Contains("this.myDateTime = _data[\"MyDateTime\"] ? new Date(_data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : <any>undefined;", code);
        }

        [Fact]
        public void When_date_handling_is_date_then_date_property_are_generated_in_interface()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date 
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("MyDateTime: Date;", code);
        }

        [Fact]
        public void When_date_handling_is_moment_then_moment_property_are_generated_in_interface()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("MyDateTime: moment.Moment;", code);
        }


        [Fact]
        public void When_date_handling_is_string_then_string_property_are_generated_in_interface()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithDateTimeProperty>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("MyDateTime: string;", code);
        }
    }
}
