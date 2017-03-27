using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;
using System.Threading.Tasks;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ExtensionCodeTests
    {
        private const string Code =
@"/// <reference path=""../../typings/angularjs/angular.d.ts"" />

import generated = require(""foo/bar"");
import foo = require(""foo/bar"");
import bar = require(""foo/bar"");

export class Bar extends generated.Bar {
    get title() {
        return this.firstName + ' ' + this.lastName;
    }
}

var clientClasses = {clientClasses};
for (var clientClass in clientClasses) {
    if (clientClasses.hasOwnProperty(clientClass)) {
        angular.module('app').service(clientClass, ['$http', clientClasses[clientClass]]);
    } 
}

class Foo extends generated.Foo {
    get title() {
        ignore(); // ignore
        return this.bar ? this.bar.title : '';
    }
}

export class Test {
    doIt() {
    }
}

var x = 10;";

        [TestMethod]
        public void When_extension_code_is_processed_then_code_and_classes_are_correctly_detected_and_converted()
        {
            //// Arrange
            var code = Code;

            //// Act
            var ext = new TypeScriptExtensionCode(code, new[] { "Foo", "Bar" });

            //// Assert
            Assert.IsTrue(ext.ExtensionClasses.ContainsKey("Foo"));
            Assert.IsTrue(ext.ExtensionClasses["Foo"].StartsWith("export class Foo extends Foo {"));

            Assert.IsTrue(ext.ExtensionClasses.ContainsKey("Bar"));
            Assert.IsTrue(ext.ExtensionClasses["Bar"].StartsWith("export class Bar extends Bar {"));

            Assert.IsTrue(ext.ImportCode.Contains("<reference path"));
            Assert.IsTrue(ext.ImportCode.Contains("import foo = require(\"foo/bar\")"));
            Assert.IsTrue(ext.ImportCode.Contains("import bar = require(\"foo/bar\")"));

            Assert.IsTrue(ext.BottomCode.StartsWith("var clientClasses"));
            Assert.IsTrue(ext.BottomCode.Contains("if (clientClasses.hasOwnProperty(clientClass))"));
            Assert.IsTrue(ext.BottomCode.Contains("export class Test"));
            Assert.IsTrue(ext.BottomCode.EndsWith("var x = 10;"));

            var body = ext.GetExtensionClassBody("Foo");
            Assert.IsTrue(body.Contains("get title() {"));
            Assert.IsFalse(body.Contains("ignore();"));
        }

        public class Foo
        {
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [TestMethod]
        public async Task When_classes_have_extension_code_then_class_body_is_copied()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                ExtendedClasses = new[] { "Foo", "Bar" },
                ExtensionCode = Code
            });
            var code = generator.GenerateFile();

            //// Assert
            Assert.IsFalse(code.Contains("FooBase"));
            Assert.IsFalse(code.Contains("BarBase"));
            Assert.IsFalse(code.Contains("generated."));

            Assert.IsTrue(code.Contains("return this.firstName + ' ' + this.lastName;"));
            Assert.IsTrue(code.Contains("return this.bar ? this.bar.title : '';"));
        }
    }
}
