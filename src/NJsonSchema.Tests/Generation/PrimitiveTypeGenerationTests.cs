using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NodaTime;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class PrimitiveTypeGenerationTests
    {
        public class Foo
        {
            [Required]
            public byte[] Bytes { get; set; }

            public byte Byte { get; set; }

            public TimeSpan TimeSpan { get; set; }

            [Required]
            public Type Type { get; set; }

            public LocalDate Date { get; set; }

            public ZonedDateTime ZonedDateTime { get; set; }

            public OffsetDateTime OffsetDateTime { get; set; }

            public Duration Duration { get; set; }
        }

        [Fact]
        public async Task When_property_is_byte_array_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Bytes"].Type);
            Assert.Equal(JsonFormatStrings.Byte, schema.Properties["Bytes"].Format);
        }

        [Fact]
        public async Task When_property_is_byte_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.Integer, schema.Properties["Byte"].Type);
            Assert.Equal(JsonFormatStrings.Byte, schema.Properties["Byte"].Format);
        }

        [Fact]
        public async Task When_property_is_timespan_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["TimeSpan"].Type);
            Assert.Equal(JsonFormatStrings.TimeSpan, schema.Properties["TimeSpan"].Format);
        }

        [Fact]
        public async Task When_property_is_type_then_schema_type_is_string()
        {
            //// Arrange
            var data = JsonConvert.SerializeObject(new Foo { Type = typeof(Foo) }); // Type property is serialized as string

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Type"].Type);
        }

        [Fact]
        public async Task When_property_is_localdate_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Date"].Type);
            Assert.Equal(JsonFormatStrings.Date, schema.Properties["Date"].Format);
        }

        [Fact]
        public async Task When_property_is_zoneddatetime_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["ZonedDateTime"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["ZonedDateTime"].Format);
        }

        [Fact]
        public async Task When_property_is_offsetdatetime_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["OffsetDateTime"].Type);
            Assert.Equal(JsonFormatStrings.DateTime, schema.Properties["OffsetDateTime"].Format);
        }

        [Fact]
        public async Task When_property_is_duration_then_schema_type_is_string()
        {
            //// Arrange


            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Assert
            Assert.Equal(JsonObjectType.String, schema.Properties["Duration"].Type);
            Assert.Equal(JsonFormatStrings.TimeSpan, schema.Properties["Duration"].Format);
        }
    }
}