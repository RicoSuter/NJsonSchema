using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NJsonSchema.Generation;
using Xunit;
// ReSharper disable Xunit.XunitTestWithConsoleOutput
// ReSharper disable PossibleNullReferenceException
#pragma warning disable 1591

namespace JetBrains.Annotations
{
    [AttributeUsage(AttributeTargets.All)] public sealed class NotNullAttribute : Attribute { }
}

namespace NJsonSchema.Tests.Generation
{
    using JetBrains.Annotations;

    public class InterfaceTests
    {
        // Note: we DON'T add the `DataContract` attribute to the class -- that way all properties are used without additional markup.
        public class BusinessCategory : ICategory
        {
            [DataMember(IsRequired = true)]
            public string Key { get; set; }

            public string DisplayName { get; set; }

            public IEnumerable<BusinessCategory> Children { get; set; }

            public IEnumerable<ICategory> Elements { get; set; }
        }

        public interface ICategory
        {
            string DisplayName { get; set; }

            [DataMember(IsRequired = true)]
            string Key { get; set; }
        }
        
        public interface ICategoryWithJetBrainsAttribute
        {
            string DisplayName { get; set; }

            [NotNull]
            string Key { get; set; }
        }

        [Fact]
        public async Task Properties_for_interface_can_be_generated_directly ()
        {
            var schema = await JsonSchema4.FromTypeAsync<ICategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true, FlattenInheritanceHierarchy = true
            });

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.NotNull(schema.ActualProperties["DisplayName"]);
            Assert.NotNull(schema.ActualProperties["Key"]);
        }

        [Fact]
        public async Task Interface_properties_with_class_values_can_be_marked_as_required_when_flattening_hierarchy ()
        {
            var schema = await JsonSchema4.FromTypeAsync<ICategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true, FlattenInheritanceHierarchy = true
            });

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.True(schema.Properties["Key"].IsRequired);
        }
        
        [Fact]
        public async Task Interface_properties_with_class_values_can_be_marked_as_required_when_flattening_hierarchy_using_jetbrains_annotations ()
        {
            var schema = await JsonSchema4.FromTypeAsync<ICategoryWithJetBrainsAttribute>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true, FlattenInheritanceHierarchy = true
            });

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.True(schema.Properties["Key"].IsRequired);
        }
        
        [Fact]
        public async Task Class_properties_with_class_values_can_be_marked_as_required_when_flattening_hierarchy ()
        {

            var schema = await JsonSchema4.FromTypeAsync<BusinessCategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true, FlattenInheritanceHierarchy = true
            });

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.True(schema.Properties["Key"].IsRequired);
        }

        [Fact]
        public async Task Interface_properties_with_class_values_can_be_marked_as_required ()
        {

            var schema = await JsonSchema4.FromTypeAsync<ICategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true//, FlattenInheritanceHierarchy = true
            });

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.True(schema.Properties["Key"].IsRequired);
        }

        
        [Fact]
        public async Task Class_properties_with_class_values_can_be_marked_as_required ()
        {
            var schema = await JsonSchema4.FromTypeAsync<BusinessCategory>();

            var json = schema.ToJson();

            Console.WriteLine(json);
            Assert.True(schema.Properties["Key"].IsRequired);
        }

        [Fact]
        public async Task When_class_inherits_from_interface_then_properties_for_interface_are_generated()
        {
            //// Arrange

            //// Act
            var schema = await JsonSchema4.FromTypeAsync<BusinessCategory>(new JsonSchemaGeneratorSettings
            {
                GenerateAbstractProperties = true
            });

            var _ = schema.ToJson();

            //// Assert
            Assert.Equal(2, schema.Definitions["ICategory"].Properties.Count);
        }
    }
}
