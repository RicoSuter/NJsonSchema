using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class IgnoredPropertyTests
    {
        public class Mno
        {
            public string IncludeMe;

            [JsonIgnore]
            public string IgnoreMe;
        }


        [Fact]
        public async Task When_field_has_JsonIgnoreAttribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Mno>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.DoesNotContain("IgnoreMe", json);
        }

        [DataContract]
        public class Xyz
        {
            [DataMember]
            public string IncludeMe;

            public string IgnoreMe;
        }

        [Fact]
        public async Task When_field_has_no_DataMemberAttribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Xyz>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.DoesNotContain("IgnoreMe", json);
        }

        [Serializable]
        public class Foo
        {
            public int Id { get; set; }

            public Dictionary<string, object> DynamicValues { get; set; }

            [JsonIgnore]
            public object this[string key]
            {
                get { throw new NotImplementedException(); }
            }

            public Foo()
            {
                DynamicValues = new Dictionary<string, object>();
            }
        }

        [Fact]
        public async Task When_indexer_property_has_ignore_attribute_then_it_is_ignored()
        {
            //// Arrange
            var schema = await JsonSchema4.FromTypeAsync<Foo>();

            //// Act
            var json = schema.ToJson();

            //// Assert
            Assert.Equal(2, schema.Properties.Count);
        }
    }
}
