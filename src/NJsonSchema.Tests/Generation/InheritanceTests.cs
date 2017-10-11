using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class InheritanceTests
    {
        [Fact]
        public async Task When_more_properties_are_defined_in_allOf_and_type_none_then_all_of_contains_all_properties()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'x-typeName': 'Foo', 
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'allOf': [
                    {
                        'type': 'object', 
                        'properties': { 
                            'baseProperty' : { 'type' : 'string' } 
                        }
                    },
                    {
                        'properties': { 
                            'prop2' : { 'type' : 'string' } 
                        }
                    }
                ]
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Assert
            Assert.NotNull(schema.InheritedSchema);
            Assert.Equal(2, schema.ActualProperties.Count);
            Assert.True(schema.ActualProperties.ContainsKey("prop1"));
            Assert.True(schema.ActualProperties.ContainsKey("prop2"));
        }

        [Fact]
        public async Task When_allOf_schema_is_object_type_then_it_is_an_inherited_schema()
        {
            //// Arrange
            var json = @"{
                '$schema': 'http://json-schema.org/draft-04/schema#',
                'type': 'object',
                'x-typeName': 'Foo', 
                'properties': { 
                    'prop1' : { 'type' : 'string' } 
                },
                'allOf': [
                    {
                        'type': 'object', 
                        'x-typeName': 'Bar', 
                        'properties': { 
                            'prop2' : { 'type' : 'string' } 
                        }
                    }
                ]
            }";

            //// Act
            var schema = await JsonSchema4.FromJsonAsync(json);

            //// Assert
            Assert.NotNull(schema.InheritedSchema);
            Assert.Equal(1, schema.ActualProperties.Count);
            Assert.True(schema.ActualProperties.ContainsKey("prop1"));
        }

        [Fact]
        public async Task When_generating_type_with_inheritance_then_allOf_has_one_item()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Teacher>();

            //// Assert
            Assert.NotNull(schema.Properties["Class"]);

            Assert.Equal(1, schema.AllOf.Count);
            Assert.True(schema.Definitions.Any(d => d.Key == "Person"));
            Assert.NotNull(schema.AllOf.First().ActualSchema.Properties["Name"]);
        }

        public class Teacher : Person
        {
            public string Class { get; set; }
        }

        public class Person
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task When_generating_type_with_inheritance_and_flattening_then_schema_has_all_properties_of_inherited_classes()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<CC>(new JsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true
            });
            var data = schema.ToJson();

            //// Assert
            Assert.Equal(4, schema.Properties.Count);
        }

        public abstract class AA
        {
            public string FirstName { get; set; }
            public abstract int Age { get; set; }
        }

        public class BB : AA
        {
            public string LastName { get; set; }
            public override int Age { get; set; }
        }

        public class CC : BB
        {
            public string Address { get; set; }
        }

        public class Dog : Animal
        {
            public string Foo { get; set; }
        }

        public class Horse : Animal
        {
            public string Bar { get; set; }
        }

        [KnownType(typeof(Dog))]
        [KnownType(typeof(Horse))]
        [JsonConverter(typeof(JsonInheritanceConverter), "kind")]
        public class Animal
        {
            public string Baz { get; set; }
        }

        [Fact]
        public async Task When_root_schema_is_inherited_then_schema_is_generated()
        {
            //// Arrange
            

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<Animal>();
            var data = schema.ToJson();

            //// Assert
            Assert.NotNull(data);
        }
    }
}
