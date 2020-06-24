using System;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using Xunit;

namespace NJsonSchema.CodeGeneration.Tests.CSharp
{
    public class ObsoleteTests
    {
        public class ObsoletePropertyTestClass
        {
            [Obsolete]
            public string Property { get; set; }
        }

        public class ObsoletePropertyWithMessageTestClass
        {
            [Obsolete("Reason property is obsolete")]
            public string Property { get; set; }
        }

        [Obsolete]
        public class ObsoleteTestClass
        {
            public string Property { get; set; }
        }

        [Obsolete("Reason class is obsolete")]
        public class ObsoleteWithMessageTestClass
        {
            public string Property { get; set; }
        }

        [Fact]
        public async Task When_property_is_obsolete_then_obsolete_attribute_is_rendered()
        {
            //// Arrange
            var schema = JsonSchema.FromType<ObsoletePropertyTestClass>();
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("[System.Obsolete]", code);
            Assert.Contains("public string Property { get; set; }", code);
        }

        [Fact]
        public async Task When_property_is_obsolete_with_a_message_then_obsolete_attribute_with_a_message_is_rendered()
        {
            //// Arrange
            var schema = JsonSchema.FromType<ObsoletePropertyWithMessageTestClass>();
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("[System.Obsolete(\"Reason property is obsolete\")]", code);
            Assert.Contains("public string Property { get; set; }", code);
        }

        [Fact]
        public async Task When_class_is_obsolete_then_obsolete_attribute_is_rendered()
        {
            //// Arrange
#pragma warning disable 612
            var schema = JsonSchema.FromType<ObsoleteTestClass>();
#pragma warning restore 612
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("[System.Obsolete]", code);
            Assert.Contains("public partial class ObsoleteTestClass", code);
        }

        [Fact]
        public async Task When_class_is_obsolete_with_a_message_then_obsolete_attribute_with_a_message_is_rendered()
        {
            //// Arrange
#pragma warning disable 618
            var schema = JsonSchema.FromType<ObsoleteWithMessageTestClass>();
#pragma warning restore 618
            var generator = new CSharpGenerator(schema);

            //// Act
            var code = generator.GenerateFile();

            //// Assert
            Assert.Contains("[System.Obsolete(\"Reason property is obsolete\")]", code);
            Assert.Contains("public partial class ObsoleteWithMessageTestClass", code);
        }
    }
}
