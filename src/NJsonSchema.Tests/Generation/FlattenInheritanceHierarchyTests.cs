using Newtonsoft.Json.Converters;
using NJsonSchema.Annotations;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class FlattenInheritanceHierarchyTests
    {
        public class Person
        {
            public string Name { get; set; }

            public List<string> Schedule { get; set; }
        }

        public class Teacher : Person
        {
            public string Class { get; set; }

            public new List<Schedule> Schedule { get; set; }
        }

        public class Schedule
        {
            int a { get; set; }

            int b { get; set; }
        }

        [Fact]
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                },
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType(typeof(Teacher), settings);
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Properties.ContainsKey("Name"));
            Assert.True(schema.Properties.ContainsKey("Class"));
        }

        public interface IFoo : IBar, IBaz
        {
            string Foo { get; set; }
        }

        public interface IBar
        {
            string Bar { get; set; }
        }

        public interface IBaz
        {
            string Baz { get; set; }
        }

        public interface ISame
        {
            string Bar { get; set; }
        }

        [Fact]
        public async Task When_FlattenInheritanceHierarchy_is_enabled_then_all_interface_properties_are_in_one_schema()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                SerializerSettings =
                {
                    Converters = { new StringEnumConverter() }
                },
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<IFoo>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(3, schema.Properties.Count);
            Assert.True(schema.Properties.ContainsKey("Foo"));
            Assert.True(schema.Properties.ContainsKey("Bar"));
            Assert.True(schema.Properties.ContainsKey("Baz"));
        }

        public class Test
        {
            public int? Id { get; set; }

            public Metadata Info { get; set; }
        }

        public class Metadata : Dictionary<string, string> { }

        [Fact]
        public async Task When_class_inherits_from_dictionary_then_flatten_inheritance_and_generate_abstract_properties_works()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true,
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Test>(settings);
            var data = schema.ToJson();

            //// Assert
        }

        [Fact]
        public async Task When_class_inherits_from_dictionary_then_flatten_inheritance_works()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true
            };

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Test>(settings);
            var data = schema.ToJson();

            //// Assert
        }

        public class A : B
        {
            public string Aaa { get; set; }
        }

        [JsonSchemaFlatten]
        public class B : C
        {
            public string Bbb { get; set; }
        }

        public class C
        {
            public string Ccc { get; set; }
        }

        [Fact]
        public async Task When_JsonSchemaFlattenAttribute_is_used_on_class_then_inherited_classed_are_merged()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings();

            //// Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<A>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Definitions.ContainsKey("B"));
            Assert.False(schema.Definitions.ContainsKey("C"));

            Assert.True(schema.ActualProperties.ContainsKey("Aaa"));
            Assert.True(schema.Definitions["B"].Properties.ContainsKey("Bbb"));
            Assert.True(schema.Definitions["B"].Properties.ContainsKey("Ccc"));
        }

        [Fact]
        public async Task When_class_inherited_and_json_flattened_then_ignore_base_property_with_same_name()
        {
            //// Arrange
            var settings = new NewtonsoftJsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true,
            };

            //// Act 
            var TeacherSchema = NewtonsoftJsonSchemaGenerator.FromType(typeof(Teacher), settings);
            var TacherData = TeacherSchema.ToJson();

            var PersonSchema = NewtonsoftJsonSchemaGenerator.FromType(typeof(Person), settings);
            var PersonData = PersonSchema.ToJson();

            //// Assert
            // Teacher correct schema
            Assert.True(TeacherSchema.Definitions.ContainsKey("Schedule"));
            Assert.True(TeacherSchema.Properties["Schedule"].Item.HasReference);

            // Person correct schema
            Assert.False(PersonSchema.Properties["Schedule"].Item.HasReference);
            Assert.True(PersonSchema.Properties["Schedule"].Item.Type == JsonObjectType.String);
        }
    }
}
