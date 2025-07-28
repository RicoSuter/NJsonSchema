using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

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
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_class(bool convertDateToLocalTimezone)
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
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_class(bool convertDateToLocalTimezone)
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
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            TypeScriptCompiler.AssertCompile("import * as moment from 'moment';" + Environment.NewLine + code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_offset_moment_then_moment_property_are_generated_in_class(bool convertDateToLocalTimezone)
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
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            TypeScriptCompiler.AssertCompile("import * as moment from 'moment';" + Environment.NewLine + code);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_dayjs_then_dayjs_property_are_generated_in_class(bool convertDateToLocalTimezone)
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
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            TypeScriptCompiler.AssertCompile("import * as dayjs from 'dayjs';" + Environment.NewLine + code);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_class(bool convertDateToLocalTimezone)
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
            await VerifyHelper.Verify(code).UseParameters(convertDateToLocalTimezone);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_date_handling_is_date_then_date_property_are_generated_in_interface()
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
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }

        [Fact]
        public async Task When_date_handling_is_moment_then_moment_property_are_generated_in_interface()
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
            await VerifyHelper.Verify(code);
        }


        [Fact]
        public async Task When_date_handling_is_string_then_string_property_are_generated_in_interface()
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
            await VerifyHelper.Verify(code);
            TypeScriptCompiler.AssertCompile(code);
        }
    }
}
