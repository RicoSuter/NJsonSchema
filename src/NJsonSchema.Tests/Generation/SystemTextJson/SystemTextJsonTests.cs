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

        public class PropertyOrderClass
        {
            [System.Text.Json.Serialization.JsonPropertyOrder(2)]
            public string B { get; set; }

            [System.Text.Json.Serialization.JsonPropertyOrder(1)]
            public string A { get; set; }

            public string C { get; set; }
        }

        [Fact]
        public void When_JsonPropertyOrder_is_set_then_properties_are_sorted_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<PropertyOrderClass>();

            // Assert
            var keys = schema.Properties.Keys.ToList();
            Assert.Equal("C", keys[0]); // default order (0)
            Assert.Equal("A", keys[1]); // order 1
            Assert.Equal("B", keys[2]); // order 2
        }

#if NET7_0_OR_GREATER
        public class ClassWithRequiredKeyword
        {
            public required string Name { get; set; }
            public string Optional { get; set; }
        }

        [Fact]
        public void When_property_has_required_keyword_then_it_is_required_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ClassWithRequiredKeyword>();

            // Assert
            Assert.Contains("Name", schema.RequiredProperties);
            Assert.DoesNotContain("Optional", schema.RequiredProperties);
        }

#nullable enable
        public class ClassWithRequiredNullableKeyword
        {
            public required string? Name { get; set; }
            public string? Optional { get; set; }
        }
#nullable restore

        [Fact]
        public void When_property_has_required_keyword_and_nullable_type_then_it_is_required_and_nullable_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ClassWithRequiredNullableKeyword>();

            // Assert: required keyword adds to required array
            Assert.Contains("Name", schema.RequiredProperties);
            Assert.DoesNotContain("Optional", schema.RequiredProperties);
            // Assert: nullable type is preserved — required keyword alone must not suppress nullability
            Assert.True(schema.Properties["Name"].IsNullable(SchemaType.JsonSchema));
            Assert.Null(schema.Properties["Name"].MinLength);
        }

        public class ClassWithJsonRequired
        {
            [System.Text.Json.Serialization.JsonRequired]
            public string Name { get; set; }
            public string Optional { get; set; }
        }

        [Fact]
        public void When_property_has_JsonRequired_then_it_is_required_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ClassWithJsonRequired>();

            // Assert
            Assert.Contains("Name", schema.RequiredProperties);
            Assert.DoesNotContain("Optional", schema.RequiredProperties);
        }
#endif

        public class ClassWithPublicField
        {
            public string MyField;
            public string MyProperty { get; set; }
        }

        [Fact]
        public void When_IncludeFields_is_true_then_public_fields_are_in_schema()
        {
            // Act
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new System.Text.Json.JsonSerializerOptions { IncludeFields = true }
            };
            var schema = JsonSchema.FromType<ClassWithPublicField>(settings);

            // Assert
            Assert.True(schema.Properties.ContainsKey("MyField"));
            Assert.True(schema.Properties.ContainsKey("MyProperty"));
        }

        [Fact]
        public void When_IncludeFields_is_false_then_public_fields_are_not_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ClassWithPublicField>();

            // Assert
            Assert.False(schema.Properties.ContainsKey("MyField"));
            Assert.True(schema.Properties.ContainsKey("MyProperty"));
        }

        public class ClassWithJsonIncludeField
        {
            [System.Text.Json.Serialization.JsonInclude]
            public string IncludedField;

            public string ExcludedField;
        }

        [Fact]
        public void When_field_has_JsonInclude_then_it_is_in_schema()
        {
            // Act
            var schema = JsonSchema.FromType<ClassWithJsonIncludeField>();

            // Assert
            Assert.True(schema.Properties.ContainsKey("IncludedField"));
            Assert.False(schema.Properties.ContainsKey("ExcludedField"));
        }
    }
}
