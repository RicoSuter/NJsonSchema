using Newtonsoft.Json;
using NJsonSchema.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class InheritanceTests
    {
        public class MyContainer
        {
            public EmptyClassInheritingDictionary CustomDictionary { get; set; }
        }

        public sealed class EmptyClassInheritingDictionary : Dictionary<string, object>
        {
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task When_empty_class_inherits_from_dictionary_then_allOf_inheritance_still_works(bool inlineNamedDictionaries)
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<MyContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.0m,
                InlineNamedDictionaries = inlineNamedDictionaries
            });

            //// Act
            var code = generator.GenerateFile("ContainerClass");

            //// Assert
            var dschema = schema.Definitions["EmptyClassInheritingDictionary"];
            Assert.Equal(0, dschema.AllOf.Count);
            Assert.True(dschema.IsDictionary);

            if (inlineNamedDictionaries)
            {
                Assert.Contains("customDictionary: { [key: string] : any; } | undefined;", code);
                Assert.DoesNotContain("EmptyClassInheritingDictionary", code);
            }
            else
            {
                Assert.Contains("customDictionary: EmptyClassInheritingDictionary", code);
                Assert.Contains("[key: string]: any;", code);
            }
        }

        [KnownType(typeof(MyException))]
        [JsonConverter(typeof(JsonInheritanceConverter), "kind")]
        public class ExceptionBase : Exception
        {
            public string Foo { get; set; }
        }

        public class MyException : ExceptionBase
        {
            public string Bar { get; set; }
        }

        public class ExceptionContainer
        {
            public ExceptionBase Exception { get; set; }
        }

        [Fact]
        public async Task When_class_with_discriminator_has_base_class_then_csharp_is_generated_correctly()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<ExceptionContainer>();
            var data = schema.ToJson();

            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2.0m });

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("class ExceptionBase extends Exception", code);
            Assert.Contains("class MyException extends ExceptionBase", code);
        }
    }
}
