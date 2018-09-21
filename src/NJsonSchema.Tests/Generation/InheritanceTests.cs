using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using NJsonSchema.Generation;
using NJsonSchema.Generation.SchemaProcessors;
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
            Assert.Contains(schema.Definitions, d => d.Key == "Person");
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

        [DataContract]
        [KnownType(typeof(ACommonThing))]
        [KnownType(typeof(BCommonThing))]
        public abstract class CommonThingBase
        {
        }

        [DataContract]
        public class ACommonThing : CommonThingBase { }

        [DataContract]
        public class BCommonThing : CommonThingBase { }

        public class ViewModelThing
        {
            public CommonThingBase CommonThing { get; set; }
        }

        [Fact]
        public async Task When_discriminator_is_externally_defined_then_it_is_generated()
        {
            //// Arrange
            var settings = new JsonSchemaGeneratorSettings
            {
                SchemaProcessors =
                {
                    new DiscriminatorSchemaProcessor(typeof(CommonThingBase), "discriminator")
                }
            };

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<ViewModelThing>(settings);
            var data = schema.ToJson();

            //// Assert
            Assert.True(schema.Definitions.ContainsKey(nameof(CommonThingBase)));
            Assert.True(schema.Definitions.ContainsKey(nameof(ACommonThing)));
            Assert.True(schema.Definitions.ContainsKey(nameof(BCommonThing)));

            var baseSchema = schema.Definitions[nameof(CommonThingBase)];
            Assert.Equal("discriminator", baseSchema.Discriminator);
        }

        [Fact]
        public async Task When_serializing_object_with_inheritance_then_discriminator_is_added()
        {
            /// Arrange
            var thing = new ViewModelThing
            {
                CommonThing = new ACommonThing()
            };

            /// Act
            var json = JsonConvert.SerializeObject(thing, Formatting.Indented, new[]
            {
                new JsonInheritanceConverter(typeof(CommonThingBase), "discriminator")
            });

            /// Assert
            Assert.Contains("\"discriminator\": \"ACommonThing\"", json);
        }

        [Fact]
        public async Task When_deserializing_object_with_inheritance_then_correct_type_is_generated()
        {
            /// Arrange
            var json =
            @"{
              ""CommonThing"": {
                ""discriminator"": ""ACommonThing""
              }
            }";

            /// Act
            var vm = JsonConvert.DeserializeObject<ViewModelThing>(json, new JsonSerializerSettings
            {
                Converters = new[]
                {
                    new JsonInheritanceConverter(typeof(CommonThingBase), "discriminator")
                }
            });

            /// Assert
            Assert.Equal(typeof(ACommonThing), vm.CommonThing.GetType());
        }

        [KnownType(typeof(InheritedClass_WithStringDiscriminant))]
        [JsonConverter(typeof(JsonInheritanceConverter), nameof(Kind))]
        public class BaseClass_WithStringDiscriminant
        {
            public string Kind { get; set; }
        }

        public class InheritedClass_WithStringDiscriminant : BaseClass_WithStringDiscriminant
        {

        }

        [Fact]
        public async Task Existing_string_property_can_be_discriminant()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<BaseClass_WithStringDiscriminant>();

            //// Assert
            Assert.NotNull(schema.Properties["Kind"]);
        }

        [KnownType(typeof(InheritedClass_WithIntDiscriminant))]
        [JsonConverter(typeof(JsonInheritanceConverter), nameof(Kind))]
        public class BaseClass_WithIntDiscriminant
        {
            public int Kind { get; set; }
        }

        public class InheritedClass_WithIntDiscriminant : BaseClass_WithStringDiscriminant
        {

        }

        [Fact]
        public async Task Existing_non_string_property_cant_be_discriminant()
        {
            //// Arrange

            //// Act
            Task<JsonSchema4> getSchema() => JsonSchema4.FromTypeAsync<BaseClass_WithIntDiscriminant>();

            //// Assert
            await Assert.ThrowsAsync<InvalidOperationException>(getSchema);
        }
    }
}
