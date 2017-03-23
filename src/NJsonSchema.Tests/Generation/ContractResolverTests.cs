using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using System;
using System.Collections;
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
            Assert.AreEqual(JsonObjectType.String | JsonObjectType.Null, schema.Properties["location"].Type,
                "Location is resolved to a string contract because it has a type converter");
        }

        /// <summary>
        /// A contract resolver that
        ///  - camel cases properties
        ///  - does not serialize properties that are read only. 
        ///  - overrides the array contract if it has a string type converter
        /// </summary>
        public class CustomContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (!prop.Writable && member.GetCustomAttribute<JsonPropertyAttribute>(true) == null)
                    prop.ShouldSerialize = o => false;
                return prop;
            }

            protected override JsonContract CreateContract(Type objectType)
            {
                JsonContract contract = base.CreateContract(objectType);

                // by default a type that can convert to string and that is also an enum will have an array contract, but serialize to a string!. fix  this
                if (contract is JsonArrayContract && typeof(IEnumerable).IsAssignableFrom(objectType) && CanNonSystemTypeDescriptorConvertString(objectType, out var converter))
                    contract = CreateStringContract(objectType);

                return contract;
            }

            static HashSet<string> _systemConverters = new HashSet<string>(new[] {
            "System.ComponentModel.ComponentConverter",
            "System.ComponentModel.ReferenceConverter",
            "System.ComponentModel.CollectionConverter" });

            public static bool CanNonSystemTypeDescriptorConvertString(Type type, out TypeConverter typeConverter)
            {
                typeConverter = TypeDescriptor.GetConverter(type);

                // use the objectType's TypeConverter if it has one and can convert to a string
                if (typeConverter != null)
                {
                    Type converterType = typeConverter.GetType();
                    if (!_systemConverters.Contains(converterType.FullName) && converterType != typeof(TypeConverter))
                        return typeConverter.CanConvertTo(typeof(string));
                }
                return false;
            }

        }

        public class Person
        {
            public string FirstName { get; set; }
            public int NameLength => FirstName.Length;
            public LocationPath Location { get; set; }
        }

        /// <summary>
        /// A class that with a custom converter could serialize to a string.
        /// NOTE: The default contract resolver would resolve this as an array contract because it implements IEnumerable
        /// </summary>
        [TypeConverter(typeof(StringConverter<LocationPath>))]
        public class LocationPath : IStringConvertable, IEnumerable<string>
        {
            public ICollection<string> Path { get; set; } = new List<string>();

            public string StringValue
            {
                get { return string.Join("/", Path); ; }
                set { Path = new List<string>(value.Split('/')); }
            }

            public IEnumerator<string> GetEnumerator() => Path.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Path.GetEnumerator();

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
