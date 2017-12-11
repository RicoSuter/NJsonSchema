using NJsonSchema.CodeGeneration.TypeScript;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ExtensionCodeTests
    {
        private const string Code =
@"/// <reference path=""../../typings/angularjs/angular.d.ts"" />

import generated = require(""foo/bar"");
import foo = require(""foo/bar"");
import bar = require(""foo/bar"");
import 'rxjs/add/operator/map';

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

export abstract class BaseClass {
    doIt() {
    }
}

var x = 10;";

        [Fact]
        public void When_extension_code_is_processed_then_code_and_classes_are_correctly_detected_and_converted()
        {
            //// Arrange
            var code = Code;

            //// Act
            var ext = new TypeScriptExtensionCode(code, new[] { "Foo", "Bar" }, new [] { "BaseClass" });

            //// Assert
            Assert.True(ext.ExtensionClasses.ContainsKey("Foo"));
            Assert.StartsWith("export class Foo extends Foo {", ext.ExtensionClasses["Foo"]);

            Assert.True(ext.ExtensionClasses.ContainsKey("Bar"));
            Assert.StartsWith("export class Bar extends Bar {", ext.ExtensionClasses["Bar"]);

            Assert.Contains("<reference path", ext.ImportCode);
            Assert.Contains("import foo = require(\"foo/bar\")", ext.ImportCode);
            Assert.Contains("import bar = require(\"foo/bar\")", ext.ImportCode);

            Assert.StartsWith("var clientClasses", ext.BottomCode);
            Assert.Contains("if (clientClasses.hasOwnProperty(clientClass))", ext.BottomCode);
            Assert.Contains("export class Test", ext.BottomCode);
            Assert.EndsWith("var x = 10;", ext.BottomCode);

            Assert.Contains("export abstract class BaseClass", ext.TopCode);

            var body = ext.GetExtensionClassBody("Foo");
            Assert.Contains("get title() {", body);
            Assert.DoesNotContain("ignore();", body);
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

        [Fact]
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
            Assert.DoesNotContain("FooBase", code);
            Assert.DoesNotContain("BarBase", code);
            Assert.DoesNotContain("generated.", code);

            Assert.Contains("return this.firstName + ' ' + this.lastName;", code);
            Assert.Contains("return this.bar ? this.bar.title : '';", code);
        }
    }
}
