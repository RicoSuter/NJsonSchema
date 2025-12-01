using System.ComponentModel.DataAnnotations;
using NJsonSchema.Annotations;
using NJsonSchema.CodeGeneration.Tests;
using NJsonSchema.Generation;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonTests
    {
        public class HealthCheckResult
        {
            [Required]
            public string Name { get; }

            public string Description { get; }

            private string PrivateReadOnlyProperty1 { get; }

            private string PrivateReadOnlyProperty2 => "TEST";

            public static string PublicReadOnlyStaticProperty { get; }

            private static string PrivateReadOnlyStaticProperty { get; }
        }

        [Fact]
        public async Task When_property_is_readonly_then_its_in_the_schema()
        {
            // Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            // Assert
            await VerifyHelper.Verify(data);
        }
        
        public class ContainerType1
        {
            public int Property { get; set; }
            public NestedType1 NestedType { get; set; }
        }
        
        public class NestedType1
        {
            public int NestedProperty { get; set; }
        }

        [Fact]
        public async Task When_type_is_excluded_then_it_should_not_be_in_the_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ContainerType1>(new SystemTextJsonSchemaGeneratorSettings
            {
                ExcludedTypeNames = [typeof(NestedType1).FullName]
            });
            var data = schema.ToJson();
            
            // Assert
            await VerifyHelper.Verify(data);
        }
        
        public class ContainerType2
        {
            public int Property { get; set; }
            
            [JsonSchemaIgnore]
            public NestedType2 NestedType { get; set; }
        }
        
        public class NestedType2
        {
            public int NestedProperty { get; set; }
        }
        
        [Fact]
        public async Task When_type_is_excluded_with_json_schema_ignore_attribute_then_it_should_not_be_in_the_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ContainerType2>();
            var data = schema.ToJson();
            
            // Assert
            await VerifyHelper.Verify(data);
        }

        [Fact]
        public async Task When_property_is_private_and_readonly_then_its_not_in_the_schema()
        {
            // Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            // Assert
            Assert.NotNull(data);
            Assert.False(data.Contains("PrivateReadOnlyProperty1"), data);
            Assert.False(data.Contains("PrivateReadOnlyProperty2"), data);
        }

        [Fact]
        public async Task When_property_is_static_readonly_then_its_not_in_the_schema()
        {
            // Act
            var schema = JsonSchema.FromType<HealthCheckResult>();
            var data = schema.ToJson();

            // Assert
            Assert.NotNull(data);
            Assert.False(data.Contains("PublicReadOnlyStaticProperty"), data);
            Assert.False(data.Contains("PrivateReadOnlyStaticProperty"), data);
        }
    }
}
