using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
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

        [Fact]
        public async Task When_generating_TypeScript_classes_then_output_is_correct()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });

            //// Assert
            Assert.Contains("init(data?: any) {", code);
        }

        [Fact]
        public async Task When_default_value_is_available_then_variable_is_initialized()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });

            //// Assert
            Assert.Contains("name: string;", code);
            Assert.Contains("this.name = \"foo\";", code);
        }

        [Fact]
        public async Task When_generating_TypeScript_knockout_classes_then_output_is_correct()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.KnockoutClass });

            //// Assert
            Assert.Contains("dateOfBirth = ko.observable<Date>();", code);
        }

        private static async Task<string> PrepareAsync(TypeScriptGeneratorSettings settings)
        {
            var schema = await JsonSchema4.FromTypeAsync<MyClassTest>();
            var data = schema.ToJson();

            //// Act
            var generator = new TypeScriptGenerator(schema, settings);
            var code = generator.GenerateFile("MyClass");
            return code;
        }

        [Fact]
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
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("a: string[];", code);
            Assert.Contains("this.a = [];", code);
            Assert.Contains("b: string[];", code);
        }

        [Fact]
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
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("a: { [key: string] : string; };", code);
            Assert.Contains("this.a = {};", code);
            Assert.Contains("b: { [key: string] : string; };", code);
        }

        [Fact]
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
                SchemaType = SchemaType.Swagger2
            });
            var code = generator.GenerateFile("MyClass");

            //// Assert
            Assert.Contains("a: A;", code);
            Assert.Contains("this.a = new A();", code);
            Assert.Contains("this.a = data[\"A\"] ? A.fromJS(data[\"A\"]) : new A();", code);

            Assert.Contains("b: B;", code);
            Assert.Contains("this.b = data[\"B\"] ? B.fromJS(data[\"B\"]) : <any>undefined;", code);
        }

        [Fact]
        public async Task When_export_types_is_true_add_export_before_class_and_interface()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class, ExportTypes = true });

            //// Assert
            Assert.Contains("export class Student extends Person implements IStudent {", code);
            Assert.Contains("export interface IStudent extends IPerson {", code);
            Assert.Contains("export interface IPerson {", code);
        }

        [Fact]
        public async Task When_export_types_is_false_dont_add_export_before_class_and_interface()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class, ExportTypes = false });

            //// Assert
            Assert.DoesNotContain("export class Student extends Person implements IStudent {", code);
            Assert.DoesNotContain("export interface IStudent extends IPerson {", code);
            Assert.DoesNotContain("export interface IPerson {", code);
        }

        [Fact]
        public async Task When_add_export_keyword_is_true_with_knockout_class_add_export_before_class()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.KnockoutClass, ExportTypes = true });

            //// Assert
            Assert.Contains("export class Student extends Person {", code);
        }

        [Fact]
        public async Task When_add_export_keyword_is_false_with_knockout_class_dont_add_export_before_class()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.KnockoutClass, ExportTypes = false });

            //// Assert
            Assert.DoesNotContain("export class Student extends Person {", code);
        }

        [Fact]
        public async Task When_class_is_generated_then_constructor_interfaces_are_correctly_generated()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class });

            //// Assert
            Assert.Contains("class Student extends Person implements IStudent {", code);
            Assert.Contains("interface IStudent extends IPerson {", code);
            Assert.Contains("interface IPerson {", code);
        }

        [Fact]
        public async Task When_GenerateConstructorInterface_then_no_interfaces_are_generated()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                GenerateConstructorInterface = false
            });

            //// Assert
            Assert.DoesNotContain("interface IStudent extends IPerson {", code);
            Assert.DoesNotContain("interface IPerson {", code);
        }

        [Fact]
        public async Task When_Knockout_class_is_generated_then_initializers_are_correct()
        {
            var code = await PrepareAsync(new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.KnockoutClass,
                GenerateConstructorInterface = false,
                TypeScriptVersion = 2.0m
            });

            //// Assert
            Assert.DoesNotContain("let firstName_ = data[\"FirstName\"];", code);
        }
    }
}