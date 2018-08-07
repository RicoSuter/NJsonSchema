using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class DictionaryTests
    {
        public enum PropertyName
        {
            Name,
            Gender
        }

        public class EnumKeyDictionaryTest
        {
            public Dictionary<PropertyName, string> Mapping { get; set; }

            public IDictionary<PropertyName, string> Mapping2 { get; set; }
        }

        [Fact]
        public async Task When_dictionary_key_is_enum_then_csharp_has_enum_key()
        {
            //// Act
            var schema = await JsonSchema4.FromTypeAsync<EnumKeyDictionaryTest>();
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties["Mapping"].IsDictionary);
            Assert.True(schema.Properties["Mapping"].DictionaryKey.ActualSchema.IsEnumeration);

            Assert.True(schema.Properties["Mapping2"].IsDictionary);
            Assert.True(schema.Properties["Mapping2"].DictionaryKey.ActualSchema.IsEnumeration);
        }
    }
}