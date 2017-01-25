using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class DateCodeGenerationTests
    {
        public class ClassWithDateProperty
        {
            public DateTime MyDateTime { get; set; }
        }

        [TestMethod]
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDateTime: string"));
            Assert.IsTrue(code.Contains("this.myDateTime = data[\"MyDateTime\"] !== undefined ? data[\"MyDateTime\"] : undefined;"));
            Assert.IsTrue(code.Contains("data[\"MyDateTime\"] = this.myDateTime !== undefined ? this.myDateTime : undefined;"));
        }

        [TestMethod]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDateTime: moment.Moment"));
            Assert.IsTrue(code.Contains("this.myDateTime = data[\"MyDateTime\"] ? moment(data[\"MyDateTime\"].toString()) : undefined;"));
            Assert.IsTrue(code.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : undefined;"));
        }

        [TestMethod]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDateTime: Date"));
            Assert.IsTrue(code.Contains("this.myDateTime = data[\"MyDateTime\"] ? new Date(data[\"MyDateTime\"].toString()) : undefined;"));
            Assert.IsTrue(code.Contains("data[\"MyDateTime\"] = this.myDateTime ? this.myDateTime.toISOString() : undefined;"));
        }

        [TestMethod]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date 
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("MyDateTime: Date;"));
        }

        [TestMethod]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("MyDateTime: moment.Moment;"));
        }


        [TestMethod]
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ClassWithDateProperty>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("MyDateTime: string;"));
        }
    }
}
