using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.TypeScript;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class DateCodeGenerationTests
    {
        private const string Json =
@"{
    '$schema': 'http://json-schema.org/draft-04/schema#',
	'type': 'object', 
	'properties': {
		'myDate': { 'type': 'string', 'format': 'date' },
		'myTimeSpan': { 'type': 'string', 'format': 'time-span' }
	}
}";

        [Fact]
        public async Task When_date_handling_is_string_then_string_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: string", code);
            Assert.Contains("this.myDate = data[\"myDate\"];", code);
            Assert.Contains("data[\"myDate\"] = this.myDate;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: moment.Moment", code);
            Assert.Contains("this.myDate = data[\"myDate\"] ? moment(data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.format('YYYY-MM-DD') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_duration_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myTimeSpan: moment.Duration", code);
            Assert.Contains("this.myTimeSpan = data[\"myTimeSpan\"] ? moment.duration(data[\"myTimeSpan\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myTimeSpan\"] = this.myTimeSpan ? this.myTimeSpan.format('d.hh:mm:ss.SS', { trim: false }) : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: Date", code);
            Assert.Contains("this.myDate = data[\"myDate\"] ? new Date(data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? formatDate(this.myDate) : <any>undefined;", code);
            Assert.Contains("function formatDate(", code);
        }

        [Fact]
        public async Task When_date_handling_is_offset_moment_then_date_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.OffsetMomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: moment.Moment", code);
            Assert.Contains("this.myDate = data[\"myDate\"] ? moment.parseZone(data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.format('YYYY-MM-DD') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date 
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: Date;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_is_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: moment.Moment;", code);
        }


        [Fact]
        public async Task When_date_handling_is_string_then_string_property_is_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema4.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: string;", code);
        }
    }
}
