using NJsonSchema.CodeGeneration.Tests;

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
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_string_then_string_property_is_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.String,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            CodeCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_moment_then_moment_property_is_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_duration_property_is_generated_in_class()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_luxon_then_datetime_property_is_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.Luxon,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
        }

        [Fact]
        public async Task When_date_handling_is_luxon_then_duration_property_is_generated_in_class()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.Luxon
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_dayjs_then_dayjs_property_is_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.DayJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_class()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }
        
        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_class_with_local_timezone_conversion()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                //DateTimeType = TypeScriptDateTimeType.Date,
                ConvertDateToLocalTimezone = true
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_offset_moment_then_date_property_is_generated_in_class(bool convertDateToLocalTimezone)
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                DateTimeType = TypeScriptDateTimeType.OffsetMomentJS,
                ConvertDateToLocalTimezone = convertDateToLocalTimezone
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_is_generated_in_interface()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                //DateTimeType = TypeScriptDateTimeType.Date
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_is_generated_in_interface()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.MomentJS
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_date_handling_is_string_then_string_property_is_generated_in_interface()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(Json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            await VerifyHelper.Verify(code);
            CodeCompiler.AssertCompile(code);
        }
    }
}
