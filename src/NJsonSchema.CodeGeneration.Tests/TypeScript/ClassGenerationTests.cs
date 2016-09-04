using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ClassGenerationTests
    {
        public class MyClassTest
        {
            [DefaultValue("foo")]
            public string Name { get; set; }

            public DateTime DateOfBirth { get; set; }

            public int[] PrimitiveArray { get; set; }

            public Dictionary<string, int> PrimitiveDictionary { get; set; }

            public DateTime[] DateArray { get; set; }

            public Dictionary<string, DateTime> DateDictionary { get; set; }

            public Student Reference { get; set; }

            public Student[] Array { get; set; }

            public Dictionary<string, Student> Dictionary { get; set; }
        }

        public class Student : Person
        {
            public string Study { get; set; }
        }

        public class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [TestMethod]
        public void When_generating_TypeScript_classes_then_output_is_correct()
        {
            var code = Prepare(TypeScriptTypeStyle.Class);

            //// Assert
            Assert.IsTrue(code.Contains("constructor(data?: any) {"));
        }

        [TestMethod]
        public void When_default_value_is_available_then_variable_is_initialized()
        {
            var code = Prepare(TypeScriptTypeStyle.Class);

            //// Assert
            Assert.IsTrue(code.Contains("name: string = \"foo\"; "));
        }

        [TestMethod]
        public void When_generating_TypeScript_knockout_classes_then_output_is_correct()
        {
            var code = Prepare(TypeScriptTypeStyle.KnockoutClass);

            //// Assert
            Assert.IsTrue(code.Contains("dateOfBirth = ko.observable<Date>();"));
        }

        private static string Prepare(TypeScriptTypeStyle style)
        {
            var schema = JsonSchema4.FromType<MyClassTest>();
            var data = schema.ToJson();
            var settings = new TypeScriptGeneratorSettings
            {
                TypeStyle = style
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, settings);
            var code = generator.GenerateFile();
            return code;
        }

        [TestMethod]
        public void When_array_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            //// Arrange
            var schema = new JsonSchema4
            {
                Properties =
                {
                    { "A", new JsonProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema4
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonProperty
                        {
                            Type = JsonObjectType.Array,
                            Item = new JsonSchema4
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullHandling = NullHandling.Swagger
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("a: string[] = [];"));
            Assert.IsTrue(code.Contains("b: string[];"));
        }

        [TestMethod]
        public void When_dictionary_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            //// Arrange
            var schema = new JsonSchema4
            {
                Properties =
                {
                    { "A", new JsonProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema4
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonProperty
                        {
                            Type = JsonObjectType.Object,
                            AdditionalPropertiesSchema = new JsonSchema4
                            {
                                Type = JsonObjectType.String
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullHandling = NullHandling.Swagger
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("a: { [key: string] : string; } = {};"));
            Assert.IsTrue(code.Contains("b: { [key: string] : string; };"));
        }

        [TestMethod]
        public void When_object_property_is_required_or_not_then_the_code_has_correct_initializer()
        {
            //// Arrange
            var schema = new JsonSchema4
            {
                Properties =
                {
                    { "A", new JsonProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = true
                        }
                    },
                    { "B", new JsonProperty
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {"A", new JsonProperty
                                    {
                                        Type = JsonObjectType.String
                                    }
                                }
                            },
                            IsRequired = false
                        }
                    },
                }
            };

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                NullHandling = NullHandling.Swagger
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsTrue(code.Contains("a: A = new A();"));
            Assert.IsTrue(code.Contains("this.a = data[\"A\"] ? A.fromJS(data[\"A\"]) : new A();"));

            Assert.IsTrue(code.Contains("b: B;"));
            Assert.IsTrue(code.Contains("this.b = data[\"B\"] ? B.fromJS(data[\"B\"]) : null;"));
        }
    }
}