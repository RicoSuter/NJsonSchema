using NJsonSchema.Annotations;
using NJsonSchema.CodeGeneration.TypeScript.Tests.Models;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class NullabilityTests
    {
        [Fact]
        public void Strict_nullability_in_TypeScript2()
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>(
                new NewtonsoftJsonSchemaGeneratorSettings
                {
                    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull
                });

            var json = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings { TypeScriptVersion = 2 });

            var output = generator.GenerateFile("MyClass");

            Assert.Contains("timeSpan: string;", output);
            Assert.Contains("gender: Gender;", output);
            Assert.Contains("address: Address;", output);
            Assert.Contains("this.address = new Address();", output);

            Assert.Contains("timeSpanOrNull: string | undefined;", output);
            Assert.Contains("genderOrNull: Gender | undefined;", output);
            Assert.Contains("addressOrNull: Address | undefined;", output);
        }

        [Fact]
        public async Task When_a_complex_property_is_not_required_and_not_nullable_then_default_is_undefined()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
  ""type"": ""object"", 
  ""properties"": {
    ""parent"": {
      ""$ref"": ""#/definitions/ParentDto""
    }
  },
  ""definitions"": {
    ""ParentDto"": {
      ""type"": ""object"",
      ""properties"": {
        ""child"": {
          ""$ref"": ""#/definitions/ChildDto""
        }
      }
    },
    ""ChildDto"": {
      ""type"": ""object"",
      ""properties"": {
        ""property"": {
          ""type"": ""string""
        }
      }
    }
  }
}");

            var generator = new TypeScriptGenerator(schema,
               new TypeScriptGeneratorSettings { TypeScriptVersion = 2, SchemaType = SchemaType.OpenApi3 });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.DoesNotContain(": new ChildDto();", output);
        }

        [Fact]
        public async Task When_a_complex_property_is_required_and_not_nullable_then_default_is_new_instance()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
  ""type"": ""object"", 
  ""properties"": {
    ""parent"": {
      ""$ref"": ""#/definitions/ParentDto""
    }
  },
  ""definitions"": {
    ""ParentDto"": {
      ""type"": ""object"",
      ""required"": [ ""child"" ],
      ""properties"": {
        ""child"": {
          ""$ref"": ""#/definitions/ChildDto""
        }
      }
    },
    ""ChildDto"": {
      ""type"": ""object"",
      ""properties"": {
        ""property"": {
          ""type"": ""string""
        }
      }
    }
  }
}");

            var generator = new TypeScriptGenerator(schema,
               new TypeScriptGeneratorSettings { TypeScriptVersion = 2, SchemaType = SchemaType.OpenApi3 });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains(": new ChildDto();", output);
        }

        [Fact]
        public async Task When_a_complex_property_is_nullable_then_default_is_null()
        {
            // Arrange
            var schema = await JsonSchema.FromJsonAsync(@"{
  ""type"": ""object"", 
  ""properties"": {
    ""parent"": {
      ""$ref"": ""#/definitions/ParentDto""
    }
  },
  ""definitions"": {
    ""ParentDto"": {
      ""type"": ""object"",
      ""properties"": {
        ""child"": {
          ""nullable"": true,
          ""allOf"": [
            {
              ""$ref"": ""#/definitions/ChildDto""
            }
          ]
        }
      }
    },
    ""ChildDto"": {
      ""type"": ""object"",
      ""properties"": {
        ""property"": {
          ""type"": ""string""
        }
      }
    }
  }
}");

            var generator = new TypeScriptGenerator(schema,
               new TypeScriptGeneratorSettings { TypeScriptVersion = 2, SchemaType = SchemaType.OpenApi3 });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.DoesNotContain(": new ChildDto();", output);
        }

        public class ClassWithNullableArrayItems
        {
            [NotNull]
            [ItemsCanBeNull]
            public List<string> Items { get; set; }
        }

        [Fact]
        public void When_array_item_is_nullable_then_generated_TypeScript_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithNullableArrayItems>();
            var json = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.7m,
                NullValue = TypeScriptNullValue.Null
            });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.True(schema.Properties["Items"].Item.IsNullable(SchemaType.JsonSchema));
            Assert.Contains(": (string | null)[]", output);
        }

        public class Complex
        {
            public int A { get; set; }
        }
        
        public class ClassWithComplexNullableArrayItems
        {
            [NotNull]
            [ItemsCanBeNull]
            public List<Complex> Items { get; set; }
        }

        [Fact]
        public void When_complex_array_item_is_nullable_then_generated_TypeScript_is_nullsafe()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<ClassWithComplexNullableArrayItems>();
            var json = schema.ToJson();
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeScriptVersion = 2.7m,
                NullValue = TypeScriptNullValue.Null
            });

            // Act
            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.True(schema.Properties["Items"].Item.IsNullable(SchemaType.JsonSchema));
            Assert.Contains(": (Complex | null)[]", output);
            Assert.Contains(".push(item ? item.toJSON() : null as any)", output);
        }
    }
}
