﻿using Newtonsoft.Json;
using System.Runtime.Serialization;
using NJsonSchema.NewtonsoftJson.Converters;
using NJsonSchema.NewtonsoftJson.Generation;
using Newtonsoft.Json.Converters;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class TypeScriptDiscriminatorTests
    {
        [JsonConverter(typeof(JsonInheritanceConverter), nameof(Type))]
        [KnownType(typeof(OneChild))]
        [KnownType(typeof(SecondChild))]
        public abstract class Base
        {
            public abstract EBase Type { get; }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum EBase
        {
            OneChild,
            SecondChild
        }

        public class OneChild : Base
        {
            public string A { get; }

            public override EBase Type => EBase.OneChild;
        }

        public class SecondChild : Base
        {
            public string B { get; }

            public override EBase Type => EBase.SecondChild;
        }

        public class Nested
        {
            public Base Child { get; set; }

            public ICollection<Base> Children { get; set; }
        }

        [Fact]
        public async Task When_generating_interface_contract_add_discriminator()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Nested>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true,
            });
            var data = schema.ToJson();
            var json = JsonConvert.SerializeObject(new OneChild());

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m,
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("export interface Base {\n    Type: EBase;\n}", code);
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_generating_interface_contract_add_discriminator_string_literal()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Nested>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true,
            });
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m,
                EnumStyle = TypeScriptEnumStyle.StringLiteral,
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("export interface Base {\n    Type: EBase;\n}", code);
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_parameter_is_abstract_then_generate_union_interface()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Nested>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                UseLeafType = true,
                TypeStyle = TypeScriptTypeStyle.Interface,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("export interface OneChild extends Base", code);
            Assert.Contains("export interface SecondChild extends Base", code);
            Assert.Contains("Child: OneChild | SecondChild;", code);
            Assert.Contains("Children: (OneChild | SecondChild)[];", code);
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public async Task When_parameter_is_abstract_then_generate_union_class()
        {
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Nested>();
            var data = schema.ToJson();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                UseLeafType = true,
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 1.8m
            });
            var code = generator.GenerateFile("MyClass");

            // Assert
            Assert.Contains("export class OneChild extends Base", code);
            Assert.Contains("export class SecondChild extends Base", code);
            Assert.Contains("child: OneChild | SecondChild;", code);
            Assert.Contains("children: (OneChild | SecondChild)[];", code);
            await VerifyHelper.Verify(code);
        }
    }
}
