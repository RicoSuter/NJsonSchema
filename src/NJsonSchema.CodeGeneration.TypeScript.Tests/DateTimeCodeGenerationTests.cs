using System;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.TypeScript;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class DateTimeCodeGenerationTests
    {
        public class ClassWithDateTimeProperty
        {
            public DateTime MyDateTime { get; set; }
        }

        [Fact]
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDateTime: string", code);
            Assert.Contains("this.myDateTime = data[\"MyDateTime\"];", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDateTime: moment.Moment", code);
            Assert.Contains("this.myDateTime = data[\"MyDateTime\"] ? moment(data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_offset_moment_then_moment_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.OffsetMomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDateTime: moment.Moment", code);
            Assert.Contains("this.myDateTime = data[\"MyDateTime\"] ? moment.parseZone(data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString(true) : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDateTime: Date", code);
            Assert.Contains("this.myDateTime = data[\"MyDateTime\"] ? new Date(data[\"MyDateTime\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date 
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("MyDateTime: Date;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("MyDateTime: moment.Moment;", code);
        }


        [Fact]
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateTimeProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("MyDateTime: string;", code);
        }
    }
}
