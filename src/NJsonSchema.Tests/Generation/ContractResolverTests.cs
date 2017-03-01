using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class ContractResolverTests
    {
        [TestMethod]
        public async Task Properties_should_match_custom_resolver()
        {
            var schema = await JsonSchema4.FromTypeAsync<Person>(new JsonSchemaGeneratorSettings
            {
                ContractResolver = new CustomContractResolver()
            });

            var data = schema.ToJson();

            //// Assert
            Assert.IsTrue(schema.Properties.ContainsKey("firstName"));
            Assert.AreEqual("firstName", schema.Properties["firstName"].Name);

            Assert.IsFalse(schema.Properties.ContainsKey("nameLength"));

            Assert.IsTrue(schema.Properties.ContainsKey("location"));
            Assert.AreEqual(schema.Properties["location"].Type, JsonObjectType.String | JsonObjectType.Null, 
                "Location is resolved to a string contract because it has a type converter");
        }

        public class CustomContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (!prop.Writable && member.GetCustomAttribute<JsonPropertyAttribute>(true) == null)
                    prop.ShouldSerialize = o => false;
                return prop;
            }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public int NameLength => FirstName.Length;
            public LocationPath Location { get; set; }
        }

        /// <summary>
        /// A class that with a custom converter could serialize to a string
        /// </summary>
        [TypeConverter(typeof(StringConverter<LocationPath>))]
        public class LocationPath : IStringConvertable
        {
            public ICollection<string> Path { get; set; } = new List<string>();

            public string StringValue
            {
                get => string.Join("/", Path);
                set => Path = new List<string>(value.Split('/'));
            }

            public override string ToString() => StringValue;
        }

        interface IStringConvertable
        {
            string StringValue { get; set; }
            string ToString();
        }

        class StringConverter<T> : TypeConverter where T : IStringConvertable, new()
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
                => sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                if (value is string)
                    return new T() { StringValue = value.ToString() };
                return base.ConvertFrom(context, culture, value);
            }
        }
    }
}
