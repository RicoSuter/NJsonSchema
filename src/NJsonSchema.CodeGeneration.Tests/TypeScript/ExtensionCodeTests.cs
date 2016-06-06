using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NJsonSchema.CodeGeneration.Tests.TypeScript
{
    [TestClass]
    public class ExtensionCodeTests
    {
        [TestMethod]
        public void When_extension_code_is_processed_then_code_and_classes_are_correctly_detected_and_converted()
        {
            //// Arrange
            var code =
@"
import generated = require(""foo/bar"");
import foo = require(""foo/bar"");
import bar = require(""foo/bar"");

export class Bar extends generated.BarBase {

}

var clientClasses = {clientClasses};
for (var clientClass in clientClasses) {
    if (clientClasses.hasOwnProperty(clientClass)) {
        angular.module('app').service(clientClass, ['$http', clientClasses[clientClass]]);
    } 
}

class Foo extends generated.FooBase {

}

export class Test {

}

var x = 10;";

            //// Act
            var ext = new TypeScriptExtensionCode(code, new []{ "Foo", "Bar" });

            //// Assert
            Assert.IsTrue(ext.Classes.ContainsKey("Foo"));
            Assert.IsTrue(ext.Classes["Foo"].StartsWith("export class Foo extends FooBase {"));

            Assert.IsTrue(ext.Classes.ContainsKey("Bar"));
            Assert.IsTrue(ext.Classes["Bar"].StartsWith("export class Bar extends BarBase {"));

            Assert.AreEqual("import foo = require(\"foo/bar\");\nimport bar = require(\"foo/bar\");", ext.CodeBefore);

            Assert.IsTrue(ext.CodeAfter.StartsWith("var clientClasses"));
            Assert.IsTrue(ext.CodeAfter.Contains("if (clientClasses.hasOwnProperty(clientClass))"));
            Assert.IsTrue(ext.CodeAfter.Contains("export class Test"));
            Assert.IsTrue(ext.CodeAfter.EndsWith("var x = 10;"));
        }
    }
}
