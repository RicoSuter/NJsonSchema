﻿using System.Runtime.Serialization;
using Newtonsoft.Json;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ConstructorInterfaceTests
    {
        public class Student : Person
        {
            public string Course { get; set; }
            public Car Car { get; set; }
        }

        [KnownType(typeof(Student))]
        [JsonConverter(typeof(JsonInheritanceConverter))]
        public abstract class Person
        {
            public Person Supervisor { get; set; }

            public Address Address { get; set; }

            public Car[] Cars { get; set; }

            public Dictionary<string, Skill> Skills { get; set; }

            // not supported

            public List<Car[]> Foo { get; set; }

            public Dictionary<string, Skill[]> Bar { get; set; }
        }

        public class Car
        {
            public string Foo { get; set; }
        }

        public class Address
        {
            public string Foo { get; set; }
        }

        public class Skill
        {
            public string Foo { get; set; }
        }

        [Fact]
        public void When_constructor_interface_and_conversion_code_is_generated_then_it_is_correct()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Person>(new NewtonsoftJsonSchemaGeneratorSettings());
            var json = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                GenerateConstructorInterface = true,
                ConvertConstructorInterfaceData = true,
                TypeScriptVersion = 1.8m
            });

            var output = generator.GenerateFile("MyClass");

            // Assert

            Assert.DoesNotContain("new MyClass(", output);
            // address property is converted:
            Assert.Contains("this.address = data.address && !(<any>data.address).toJSON ? new Address(data.address) : <Address>this.address;", output);
            // cars items are converted:
            Assert.Contains("this.cars[i] = item && !(<any>item).toJSON ? new Car(item) : <Car>item;", output);
            // skills values are converted:
            Assert.Contains("this.skills[key] = item && !(<any>item).toJSON ? new Skill(item) : <Skill>item;", output);
            // student car is converted:
            Assert.Contains("this.car = data.car && !(<any>data.car).toJSON ? new Car(data.car) : <Car>this.car;", output);

            // interface is correct
            Assert.Contains(@"export interface IMyClass {
    supervisor: MyClass;
    address: IAddress;
    cars: ICar[];
    skills: { [key: string]: ISkill; };
    foo: Car[][];
    bar: { [key: string]: Skill[]; };
}".Replace("\r", "").Replace("\n", ""), output.Replace("\r", "").Replace("\n", ""));
        }

        [Fact]
        public async Task When_array_of_string_dictionary_is_used_with_ConvertConstructorInterfaceData_then_it_should_be_ignored()
        {
            // Arrange
            var json = @"
{
    ""type"": ""object"",
    ""properties"": {
        ""custom4"": {
            ""type"": ""array"",
            ""items"": {
                ""type"": ""object"",
                ""additionalProperties"": {
                    ""type"": ""string""
                }
            }
        }
    }
}";
            var schema = await JsonSchema.FromJsonAsync(json);

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                GenerateConstructorInterface = true,
                ConvertConstructorInterfaceData = true

            });

            var output = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("custom4: { [key: string]: string; }[];", output);
        }
    }
}
