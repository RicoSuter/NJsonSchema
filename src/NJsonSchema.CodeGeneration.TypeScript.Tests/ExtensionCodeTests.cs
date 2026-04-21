using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.NewtonsoftJson.Generation;

namespace NJsonSchema.CodeGeneration.TypeScript.Tests
{
    public class ExtensionCodeTests
    {
        // Important: Line 18 has a \t
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

// Imported class for ...
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
            // Arrange
            var code = Code;

            // Act
            var extensionCode = new TypeScriptExtensionCode(code, ["Foo", "Bar"], ["BaseClass"]);

            // Assert
            Assert.True(extensionCode.ExtensionClasses.ContainsKey("Foo"));
            Assert.StartsWith("export class Foo extends Foo {", extensionCode.ExtensionClasses["Foo"]);

            Assert.True(extensionCode.ExtensionClasses.ContainsKey("Bar"));
            Assert.StartsWith("export class Bar extends Bar {", extensionCode.ExtensionClasses["Bar"]);

            Assert.Contains("<reference path", extensionCode.ImportCode);
            Assert.Contains("import foo = require(\"foo/bar\")", extensionCode.ImportCode);
            Assert.Contains("import bar = require(\"foo/bar\")", extensionCode.ImportCode);

            Assert.StartsWith("var clientClasses", extensionCode.BottomCode);
            Assert.Contains("if (clientClasses.hasOwnProperty(clientClass))", extensionCode.BottomCode);
            Assert.Contains("export class Test", extensionCode.BottomCode);
            Assert.EndsWith("var x = 10;", extensionCode.BottomCode);

            Assert.Contains("export abstract class BaseClass", extensionCode.TopCode);

            var body = extensionCode.GetExtensionClassBody("Foo");
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
            // Arrange
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>();

            // Act
            var generator = new TypeScriptGenerator(schema, new TypeScriptGeneratorSettings
            {
                ExtendedClasses = ["Foo", "Bar"],
                ExtensionCode = Code
            });
            var code = generator.GenerateFile();

            // Assert
            await VerifyHelper.Verify(code);
        }

        [Fact]
        public void When_extension_code_has_comment_then_it_is_processed_correctly()
        {
            // Arrange
            var code = @"// This is the class that uses HTTP

export class UseHttpCookiesForApi {
    protected transformOptions(options: RequestInit): Promise<RequestInit> {
        options.credentials = 'same-origin';
        return Promise.resolve(options);
    }
}";

            // Act
            var extensionCode = new TypeScriptExtensionCode(code, [], ["UseHttpCookiesForApi"]);

            // Assert
            Assert.Empty(extensionCode.ExtensionClasses);
            Assert.DoesNotContain("UseHttpCookiesForApi", extensionCode.BottomCode);
            Assert.Contains("UseHttpCookiesForApi", extensionCode.TopCode);
        }
    }
}
