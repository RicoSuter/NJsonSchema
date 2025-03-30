﻿using NJsonSchema.Converters;
using System.Text.Json;

#if !NETFRAMEWORK
using System.Text.Json.Serialization;
#endif

using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
#if NETFRAMEWORK
    file static class StringExtensions {
        /// <summary>
        /// Mimic .NET 6+ String.ReplaceLineEndings
        /// </summary>
        public static string ReplaceLineEndings(this string content, string lineSeparator = "\n")
        {
            return string.Join(
                lineSeparator,
                content.Replace("\r\n", "\n").Split('\r', '\n', '\f', '\u0085', '\u2028', '\u2029')
            );
        }
    }
#endif

    public class SystemTextJsonInheritanceTests
    {
        public class Apple : Fruit
        {
            public string Foo { get; set; }
        }

        public class Orange : Fruit
        {
            public string Bar { get; set; }
        }

        [JsonInheritance("a", typeof(Apple))]
        [JsonInheritance("o", typeof(Orange))]
        [JsonInheritanceConverter(typeof(Fruit), "k")]
        public class Fruit
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_using_JsonInheritanceAttribute_and_SystemTextJson_then_schema_is_correct()
        {
            // Act
            var schema = JsonSchema.FromType<Fruit>();
            var data = schema.ToJson().ReplaceLineEndings();

            // Assert
            Assert.NotNull(data);
            Assert.Contains(@"""a"": """, data);
            Assert.Contains(@"""o"": """, data);
            Assert.Contains(
                """
                      "discriminator": {
                        "propertyName": "k",
                        "mapping": {
                          "a": "#/definitions/Apple",
                          "o": "#/definitions/Orange"
                        }
                      },
                    """.ReplaceLineEndings(), data);
        }

        [Fact]
        public async Task When_discriminator_is_wrong_then_no_stackoverflow()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => // Throws "Could not find subtype of..."
            {
                JsonSerializer.Deserialize<Fruit>("{\"k\": \"invalid\"}");
            });            
        }

#if !NETFRAMEWORK

        public class Apple2 : Fruit2
        {
            public string Foo { get; set; }
        }

        public class Orange2 : Fruit2
        {
            public string Bar { get; set; }
        }

        [JsonDerivedType(typeof(Apple2), "a")]
        [JsonDerivedType(typeof(Orange2), "o")]
        [JsonPolymorphic(TypeDiscriminatorPropertyName = "k")]
        public class Fruit2
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_using_native_attributes_in_SystemTextJson_then_schema_is_correct()
        {
            // Act
            var schema = JsonSchema.FromType<Fruit2>();
            var data = schema.ToJson().ReplaceLineEndings();

            // Assert
            Assert.NotNull(data);
            Assert.Contains(@"""a"": """, data);
            Assert.Contains(@"""o"": """, data);
            Assert.Contains(
                """
                      "discriminator": {
                        "propertyName": "k",
                        "mapping": {
                          "a": "#/definitions/Apple2",
                          "o": "#/definitions/Orange2"
                        }
                      },
                    """.ReplaceLineEndings(), data);
        }

        public class Dalmation : Dog
        {
            public string Foo { get; set; }
        }

        public class Poodle : Dog
        {
            public string Bar { get; set; }
        }

        [JsonDerivedType(typeof(Dalmation), 1)]
        [JsonDerivedType(typeof(Poodle), 2)]
        [JsonPolymorphic(TypeDiscriminatorPropertyName = "breed")]
        public class Dog
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_using_native_attributes_and_integer_discriminator_in_SystemTextJson_then_schema_is_correct()
        {
            // Act
            var schema = JsonSchema.FromType<Dog>();
            var data = schema.ToJson().ReplaceLineEndings();

            // Assert
            Assert.NotNull(data);
            Assert.Contains(@"""1"": """, data);
            Assert.Contains(@"""2"": """, data);
            Assert.Contains(
                """
                      "discriminator": {
                        "propertyName": "breed",
                        "mapping": {
                          "1": "#/definitions/Dalmation",
                          "2": "#/definitions/Poodle"
                        }
                      },
                    """.ReplaceLineEndings(), data);
        }
#endif
    }
}