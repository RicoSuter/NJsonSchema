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
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: string", code);
            Assert.Contains("this.myDate = _data[\"myDate\"];", code);
            Assert.Contains("data[\"myDate\"] = this.myDate;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: moment.Moment", code);
            Assert.Contains("this.myDate = _data[\"myDate\"] ? moment(_data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.format('YYYY-MM-DD') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_duration_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myTimeSpan: moment.Duration", code);
            Assert.Contains("this.myTimeSpan = _data[\"myTimeSpan\"] ? moment.duration(_data[\"myTimeSpan\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myTimeSpan\"] = this.myTimeSpan ? this.myTimeSpan.format('d.hh:mm:ss.SS', { trim: false }) : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_luxon_then_datetime_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.Luxon
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: DateTime", code);
            Assert.Contains("this.myDate = _data[\"myDate\"] ? DateTime.fromISO(_data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.toFormat('yyyy-MM-dd') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_luxon_then_duration_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.Luxon
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myTimeSpan: Duration", code);
            Assert.Contains("this.myTimeSpan = _data[\"myTimeSpan\"] ? Duration.fromISO(_data[\"myTimeSpan\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myTimeSpan\"] = this.myTimeSpan ? this.myTimeSpan.toString() : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_dayjs_then_dayjs_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.DayJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: dayjs.Dayjs", code);
            Assert.Contains("this.myDate = _data[\"myDate\"] ? dayjs(_data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.format('YYYY-MM-DD') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: Date", code);
            Assert.Contains("this.myDate = _data[\"myDate\"] ? new Date(_data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? formatDate(this.myDate) : <any>undefined;", code);
            Assert.Contains("function formatDate(", code);
        }

        [Fact]
        public async Task When_date_handling_is_offset_moment_then_date_property_is_generated_in_class()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.OffsetMomentJS
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("myDate: moment.Moment", code);
            Assert.Contains("this.myDate = _data[\"myDate\"] ? moment.parseZone(_data[\"myDate\"].toString()) : <any>undefined;", code);
            Assert.Contains("data[\"myDate\"] = this.myDate ? this.myDate.format('YYYY-MM-DD') : <any>undefined;", code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_interface()
        {
            //// Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

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
            var schema = await JsonSchema.FromJsonAsync(Json);

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
            var schema = await JsonSchema.FromJsonAsync(Json);

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
