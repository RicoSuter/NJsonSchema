using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Generation;

namespace NJsonSchema.CodeGeneration.Tests
{
    [TestClass]
    public class AttributeIgnoreTests
    {
        public class PartiallyDeprecated
        {
            public bool BooleanProperty { get; set; }

            [Obsolete("This property is now obsolete")]
            public int IntProperty { get; set; }

            public string StringField;

            [Obsolete]
            public byte ByteField;
        }
        
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task PropertyHasDefaultAttribute_NotSerialized()
        {
            // Arrange

            // Act
            var schema = await JsonSchema4.FromTypeAsync<PartiallyDeprecated>(new JsonSchemaGeneratorSettings()
            {
                IgnoreDeprecatedProperties = true
            });

            // Assert
            #pragma warning disable 618
            Assert.IsNull(schema.Properties[nameof(PartiallyDeprecated.IntProperty)]);
            #pragma warning restore 618
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task FieldHasDefaultAttribute_NotSerialized()
        {
            // Arrange

            // Act
            var schema = await JsonSchema4.FromTypeAsync<PartiallyDeprecated>(new JsonSchemaGeneratorSettings()
            {
                IgnoreDeprecatedProperties = true
            });

            // Assert
            #pragma warning disable 612
            Assert.IsNull(schema.Properties[nameof(PartiallyDeprecated.ByteField)]);
            #pragma warning restore 612
        }
    }
}
