using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Converters;

namespace NJsonSchema.CodeGeneration.Tests
{
    public class Container
    {
        public Animal Animal { get; set; }
    }

    [JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
    [KnownType(typeof(Dog))]
    public class Animal
    {
        public string Foo { get; set; }
    }

    public class Dog : Animal
    {
        public string Bar { get; set; }
        public List<SubClass> SubElements { get; set; }
    }

    [JsonConverter(typeof(JsonInheritanceConverter), "discriminator")]
    [KnownType(typeof(SubClass1))]
    [KnownType(typeof(SubClass2))]
    public class SubClass
    {
    }

    public class SubClass1 : SubClass
    {
        public string Prop1 { get; set; }
    }

    [KnownType("GetTypes")]
    public class SubClass2 : SubClass
    {
        public string Prop2 { get; set; }

        public static Type[] GetTypes()
        {
            return new Type[] { typeof(SubClass3) };
        }
    }

    public class SubClass3 : SubClass2
    {
        public string Prop3 { get; set; }
    }

    [TestClass]
    public class InheritanceSerializationTests
    {
        [TestMethod]
        public void When_JsonInheritanceConverter_is_passed_null_it_deserializes_to_null()
        {
            //// Arrange

            //// Act
            var result = JsonConvert.DeserializeObject<SubClass>("null");

            //// Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task When_JsonInheritanceConverter_is_used_then_inheritance_is_correctly_serialized_and_deserialized()
        {
            //// Arrange
            var container = new Container
            {
                Animal = new Dog
                {
                    Foo = "foo",
                    Bar = "bar",
                    SubElements = new List<SubClass>
                    {
                        new SubClass1 { Prop1 = "x" },
                        new SubClass3 { Prop2 = "x", Prop3 = "y"}
                    }
                }
            };

            //// Act
            var json = JsonConvert.SerializeObject(container, Formatting.Indented);
            var deserializedContainer = JsonConvert.DeserializeObject<Container>(json);

            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var schemaJson = schema.ToJson();
            var errors = schema.Validate(json);

            //// Assert
            Assert.IsTrue(deserializedContainer.Animal is Dog);
            Assert.IsTrue((deserializedContainer.Animal as Dog).SubElements.First() is SubClass1);
            Assert.IsTrue((deserializedContainer.Animal as Dog).SubElements[1] is SubClass3);
        }

        [TestMethod]
        public async Task When_serializer_setting_is_changed_then_converter_uses_correct_settings()
        {
            //// Arrange
            var container = new Container
            {
                Animal = new Dog
                {
                    Foo = "foo",
                    Bar = "bar",
                    SubElements = new List<SubClass>
                    {
                        new SubClass1 { Prop1 = "x" },
                        new SubClass3 { Prop2 = "x", Prop3 = "y"}
                    }
                }
            };

            //// Act
            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var json = JsonConvert.SerializeObject(container, Formatting.Indented, settings);
            var deserializedContainer = JsonConvert.DeserializeObject<Container>(json);

            //// Assert
            Assert.IsTrue(json.Contains("prop3"));
            Assert.IsFalse(json.Contains("Prop3"));
        }

        [TestMethod]
        public void JsonInheritanceConverter_is_thread_safe()
        {
            //// Arrange
            var tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await When_JsonInheritanceConverter_is_used_then_inheritance_is_correctly_serialized_and_deserialized();
                }));
            }

            //// Act
            Task.WaitAll(tasks.ToArray());

            //// Assert
            // No exceptions
        }

        [TestMethod]
        public async Task When_JsonInheritanceConverter_is_set_then_discriminator_field_is_set()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Container>();

            //// Act
            var baseSchema = schema.Properties["Animal"].ActualPropertySchema.ActualSchema;
            var discriminator = baseSchema.Discriminator;
            var property = baseSchema.Properties["discriminator"];

            var json = schema.ToJson();

            //// Assert
            Assert.IsNotNull(property);
            Assert.IsTrue(property.IsRequired);
            Assert.AreEqual("discriminator", discriminator);
        }

        [TestMethod]
        public async Task When_schema_contains_discriminator_and_inheritance_hierarchy_then_CSharp_is_correctly_generated()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Container>();

            //// Act
            var generator = new CSharpGenerator(schema, new CSharpGeneratorSettings { ClassStyle = CSharpClassStyle.Poco });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.IsFalse(code.Contains("public string Discriminator")); // discriminator property is not generated
            Assert.IsTrue(code.Contains("[Newtonsoft.Json.JsonConverter(typeof(JsonInheritanceConverter), \"discriminator\")]")); // attribute is generated
            Assert.IsTrue(code.Contains("class JsonInheritanceConverter")); // converter is generated
        }

        [TestMethod]
        public async Task When_schema_contains_discriminator_and_inheritance_hierarchy_then_TypeScript_is_correctly_generated()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Container>();
            var json = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class
            });
            var code = generator.GenerateFile("Container");

            //// Assert
            Assert.IsTrue(code.Contains("export class Container"));
            Assert.IsTrue(code.Contains("export class Animal"));
            Assert.IsTrue(code.Contains("export class Dog"));

            // discriminator is available for deserialization
            Assert.IsTrue(code.Contains("protected _discriminator: string;")); // discriminator must be private
            Assert.IsTrue(code.Contains("new Dog();")); // type is chosen by discriminator 
            Assert.IsTrue(code.Contains("new Animal();")); // type is chosen by discriminator 

            // discriminator is assign for serialization
            Assert.IsTrue(code.Contains("this._discriminator = \"Animal\""));
            Assert.IsTrue(code.Contains("this._discriminator = \"Dog\""));
        }
    }
}