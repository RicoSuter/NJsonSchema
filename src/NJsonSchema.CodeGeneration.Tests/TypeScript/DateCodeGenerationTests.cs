using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class DateCodeGenerationTests
    {
        private const string Json =
@"{
	""type"": ""object"", 
	""properties"": {
		""myDate"": ""2017-01-01""
	}
}";

        [TestMethod]
        public void When_date_handling_is_string_then_string_property_are_generated_in_class()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: string"));
            Assert.IsTrue(code.Contains("this.myDate = data[\"myDate\"];"));
            Assert.IsTrue(code.Contains("data[\"myDate\"] = this.myDate;"));
        }

        [TestMethod]
        public void When_date_handling_is_moment_then_moment_property_are_generated_in_class()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: moment.Moment"));
            Assert.IsTrue(code.Contains("this.myDate = data[\"myDate\"] ? moment(data[\"myDate\"].toString()) : <any>undefined;"));
            Assert.IsTrue(code.Contains("data[\"myDate\"] = this.myDate ? this.myDate.toISOString().slice(0, 10) : <any>undefined;"));
        }

        [TestMethod]
        public void When_date_handling_is_date_then_date_property_are_generated_in_class()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: Date"));
            Assert.IsTrue(code.Contains("this.myDate = data[\"myDate\"] ? new Date(data[\"myDate\"].toString()) : <any>undefined;"));
            Assert.IsTrue(code.Contains("data[\"myDate\"] = this.myDate ? this.myDate.toISOString().slice(0, 10) : <any>undefined;"));
        }

        [TestMethod]
        public void When_date_handling_is_date_then_date_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date 
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: Date;"));
        }

        [TestMethod]
        public void When_date_handling_is_moment_then_moment_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: moment.Moment;"));
        }


        [TestMethod]
        public void When_date_handling_is_string_then_string_property_are_generated_in_interface()
        {
            //// Arrange
            var schema = JsonSchema4.FromData(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsTrue(code.Contains("myDate: string;"));
        }
    }
}
