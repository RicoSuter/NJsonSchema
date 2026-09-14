# JsonTypeInfo-Based Schema Generation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Use `JsonTypeInfo` contract metadata on net8.0+ for schema generation, with fallback to current reflection-based approach on older TFMs.

**Architecture:** `#if NET8_0_OR_GREATER` compile-time split in `SystemTextJsonReflectionService`. The `JsonTypeInfo` path uses `JsonSerializerOptions.GetTypeInfo()` for property discovery and bridges back to CLR members via `JsonPropertyInfo.AttributeProvider` for NJsonSchema-specific attributes. Three reflection-path fixes ship alongside (DefaultIgnoreCondition, IncludeFields+JsonInclude, generic JsonStringEnumConverter).

**Tech Stack:** .NET 8+, System.Text.Json, System.Text.Json.Serialization.Metadata, xUnit v3, Verify

**Design doc:** `docs/plans/2026-04-04-json-type-info-schema-generation-design.md`

---

## Phase 1: Reflection Path Fixes (All TFMs)

These fixes improve the existing reflection path and also apply to the new `JsonTypeInfo` path.

### Task 1: Respect `DefaultIgnoreCondition` from `JsonSerializerOptions`

**Files:**
- Test: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs`
- Modify: `src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs`

**Step 1: Write the failing test**

Add to `SystemTextJsonTests.cs`:

```csharp
[Fact]
public async Task When_DefaultIgnoreCondition_is_WhenWritingNull_then_nullable_properties_are_not_required()
{
    // Arrange
    var settings = new SystemTextJsonSchemaGeneratorSettings
    {
        SerializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        }
    };

    // Act
    var schema = JsonSchema.FromType<DefaultIgnoreConditionTestClass>(settings);

    // Assert
    Assert.DoesNotContain("NullableValue", schema.RequiredProperties);
    Assert.Contains("NonNullableValue", schema.Properties.Keys);
    Assert.Contains("NullableValue", schema.Properties.Keys);
}

public class DefaultIgnoreConditionTestClass
{
    public int NonNullableValue { get; set; }
    public string? NullableValue { get; set; }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_DefaultIgnoreCondition_is_WhenWritingNull"`
Expected: FAIL — `DefaultIgnoreCondition` is not currently respected.

**Step 3: Write minimal implementation**

In `SystemTextJsonReflectionService.cs`, in the `GenerateProperties` method, after checking `jsonIgnoreAttribute` (around line 57-68), add a check for `DefaultIgnoreCondition`:

```csharp
// After the existing jsonIgnoreAttribute check block (line 68), add:
if (!propertyIgnored && settings.SerializerOptions.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
{
    propertyIgnored = true;
}

// Also need to handle WhenWritingNull/WhenWritingDefault — these don't ignore properties
// but they affect whether the property should be considered required.
// Track this for the required-properties logic below:
var effectiveIgnoreCondition = jsonIgnoreAttribute != null
    ? jsonIgnoreAttribute.TryGetPropertyValue<object>("Condition")?.ToString()
    : settings.SerializerOptions.DefaultIgnoreCondition.ToString();
```

Then in the required-properties logic (around line 93-105), skip adding to `RequiredProperties` when the effective ignore condition is `WhenWritingNull` or `WhenWritingDefault` and the property is nullable:

```csharp
var isConditionallyIgnored = effectiveIgnoreCondition is "WhenWritingNull" or "WhenWritingDefault";
var hasRequiredAttribute = requiredAttribute != null || hasRequiredMemberAttribute || hasJsonRequiredAttribute;
if ((hasRequiredAttribute || isDataContractMemberRequired) && !isConditionallyIgnored)
{
    schema.RequiredProperties.Add(propertyName);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_DefaultIgnoreCondition_is_WhenWritingNull"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs
git commit -m "feat: respect DefaultIgnoreCondition from JsonSerializerOptions in schema generation"
```

---

### Task 2: Fix `[JsonInclude]` on private fields

**Files:**
- Test: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs`
- Modify: `src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public async Task When_private_field_has_JsonInclude_then_it_is_in_schema()
{
    // Arrange
    var settings = new SystemTextJsonSchemaGeneratorSettings();

    // Act
    var schema = JsonSchema.FromType<JsonIncludePrivateFieldTestClass>(settings);

    // Assert
    Assert.Contains("myPrivateField", schema.Properties.Keys);
}

public class JsonIncludePrivateFieldTestClass
{
    [System.Text.Json.Serialization.JsonInclude]
    [System.Text.Json.Serialization.JsonPropertyName("myPrivateField")]
    private string _myPrivateField = "test";

    public string PublicProperty { get; set; } = "hello";
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_private_field_has_JsonInclude_then_it_is_in_schema"`
Expected: FAIL — private fields with `[JsonInclude]` are currently skipped because of the `fieldInfo.IsPrivate` check.

**Step 3: Write minimal implementation**

In `SystemTextJsonReflectionService.cs`, modify the field exclusion logic at line 29-32:

```csharp
if (accessorInfo.MemberInfo is FieldInfo fieldInfo && (fieldInfo.IsStatic ||
    (fieldInfo.IsPrivate &&
        !fieldInfo.IsDefined(typeof(JsonIncludeAttribute))) ||
    (!fieldInfo.IsPrivate &&
        !fieldInfo.IsDefined(typeof(DataMemberAttribute)) &&
        !settings.SerializerOptions.IncludeFields &&
        !fieldInfo.IsDefined(typeof(JsonIncludeAttribute)))))
{
    continue;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_private_field_has_JsonInclude_then_it_is_in_schema"`
Expected: PASS

**Step 5: Run all existing tests to verify no regressions**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "SystemTextJson"`
Expected: All existing tests still pass.

**Step 6: Commit**

```bash
git add src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs
git commit -m "fix: respect [JsonInclude] on private fields in schema generation"
```

---

### Task 3: Detect generic `JsonStringEnumConverter<TEnum>`

**Files:**
- Test: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonEnumTests.cs`
- Modify: `src/NJsonSchema/Generation/ReflectionServiceBase.cs`
- Modify: `src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs`

**Step 1: Write the failing test**

Add to `SystemTextJsonEnumTests.cs`:

```csharp
[Fact]
public async Task When_enum_has_generic_JsonStringEnumConverter_attribute_then_it_is_string_enum()
{
    // Arrange
    var settings = new SystemTextJsonSchemaGeneratorSettings();

    // Act
    var schema = JsonSchema.FromType<GenericEnumConverterTestClass>(settings);

    // Assert
    var property = schema.Properties["Status"];
    Assert.True(property.ActualSchema.Type.HasFlag(JsonObjectType.String));
}

public class GenericEnumConverterTestClass
{
    public GenericEnumStatus Status { get; set; }
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<GenericEnumStatus>))]
public enum GenericEnumStatus
{
    Active,
    Inactive
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_enum_has_generic_JsonStringEnumConverter_attribute_then_it_is_string_enum"`
Expected: FAIL — generic `JsonStringEnumConverter<T>` is not detected.

**Step 3: Write minimal implementation**

In `ReflectionServiceBase.cs`, update the `HasStringEnumConverter` method (line 380-403). The current check at line 397 uses `IsAssignableToTypeName("JsonStringEnumConverter", TypeNameStyle.Name)`. The generic variant's name is `JsonStringEnumConverter`1` which should already match by name. But the existing check on line 398 uses full name matching which may miss it. Update:

```csharp
private static bool HasStringEnumConverter(ContextualType contextualType)
{
    dynamic? jsonConverterAttribute = null;
    foreach (var a in contextualType.GetContextOrTypeAttributes(true))
    {
        if (a.GetType().Name == "JsonConverterAttribute")
        {
            jsonConverterAttribute = a;
            break;
        }
    }

    if (jsonConverterAttribute != null && ObjectExtensions.HasProperty(jsonConverterAttribute, "ConverterType"))
    {
        if (jsonConverterAttribute?.ConverterType is Type converterType)
        {
            if (converterType.IsAssignableToTypeName("StringEnumConverter", TypeNameStyle.Name))
            {
                return true;
            }

            // Check non-generic JsonStringEnumConverter
            if (converterType.IsAssignableToTypeName("System.Text.Json.Serialization.JsonStringEnumConverter", TypeNameStyle.FullName))
            {
                return true;
            }

            // Check generic JsonStringEnumConverter<TEnum>
            if (converterType.IsGenericType &&
                converterType.GetGenericTypeDefinition().FullName == "System.Text.Json.Serialization.JsonStringEnumConverter`1")
            {
                return true;
            }
        }
    }

    return false;
}
```

Also update `IsStringEnum` in `SystemTextJsonReflectionService.cs` to check global generic converters:

```csharp
public override bool IsStringEnum(ContextualType contextualType, SystemTextJsonSchemaGeneratorSettings settings)
{
    var hasGlobalStringEnumConverter = settings.SerializerOptions.Converters.Any(c =>
        c is JsonStringEnumConverter ||
        (c.GetType().IsGenericType &&
         c.GetType().GetGenericTypeDefinition().FullName == "System.Text.Json.Serialization.JsonStringEnumConverter`1"));
    return hasGlobalStringEnumConverter || base.IsStringEnum(contextualType, settings);
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_enum_has_generic_JsonStringEnumConverter_attribute_then_it_is_string_enum"`
Expected: PASS

**Step 5: Run all enum tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "SystemTextJsonEnum"`
Expected: All pass.

**Step 6: Commit**

```bash
git add src/NJsonSchema/Generation/ReflectionServiceBase.cs src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonEnumTests.cs
git commit -m "fix: detect generic JsonStringEnumConverter<TEnum> in schema generation"
```

---

## Phase 2: Settings & API Surface (net8.0+)

### Task 4: Add `FallbackToReflection` setting

**Files:**
- Modify: `src/NJsonSchema/Generation/SystemTextJsonSchemaGeneratorSettings.cs`

**Step 1: Add the property**

```csharp
public class SystemTextJsonSchemaGeneratorSettings : JsonSchemaGeneratorSettings
{
    public SystemTextJsonSchemaGeneratorSettings() : base(new SystemTextJsonReflectionService())
    {
    }

    /// <summary>Gets or sets the System.Text.Json serializer options.</summary>
    [JsonIgnore]
    public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions();

#if NET8_0_OR_GREATER
    /// <summary>
    /// Gets or sets a value indicating whether to fall back to reflection-based property discovery
    /// when JsonTypeInfo resolution fails. Default is true. Set to false for strict AOT mode
    /// where you want an error if a type is not in the configured TypeInfoResolver.
    /// </summary>
    [JsonIgnore]
    public bool FallbackToReflection { get; set; } = true;
#endif
}
```

**Step 2: Verify it compiles on all TFMs**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`
Expected: Build succeeds on all target frameworks.

**Step 3: Commit**

```bash
git add src/NJsonSchema/Generation/SystemTextJsonSchemaGeneratorSettings.cs
git commit -m "feat: add FallbackToReflection setting for net8.0+"
```

---

### Task 5: Add `FromType` overloads for `JsonSerializerContext`

**Files:**
- Test: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs`
- Modify: `src/NJsonSchema/JsonSchema.cs`

**Step 1: Write the failing test**

This test needs a source-generated context. Add to `SystemTextJsonTests.cs`:

```csharp
#if NET8_0_OR_GREATER
[Fact]
public async Task When_FromType_with_JsonSerializerContext_then_schema_is_generated()
{
    // Act
    var schema = JsonSchema.FromType<ContextTestPerson>(ContextTestJsonContext.Default);

    // Assert
    Assert.Contains("firstName", schema.Properties.Keys);
    Assert.Contains("age", schema.Properties.Keys);
}

public class ContextTestPerson
{
    [System.Text.Json.Serialization.JsonPropertyName("firstName")]
    public string Name { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("age")]
    public int Age { get; set; }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(ContextTestPerson))]
public partial class ContextTestJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}
#endif
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_FromType_with_JsonSerializerContext_then_schema_is_generated"`
Expected: FAIL — compilation error, `FromType(JsonSerializerContext)` does not exist yet.

**Step 3: Write minimal implementation**

In `JsonSchema.cs`, add after the existing `FromType` overloads (around line 222):

```csharp
#if NET8_0_OR_GREATER
/// <summary>Creates a <see cref="JsonSchema"/> from a given type using a source-generated JsonSerializerContext (AOT-compatible).</summary>
/// <typeparam name="TType">The type to generate the schema for.</typeparam>
/// <param name="context">The JsonSerializerContext containing type metadata.</param>
/// <returns>The <see cref="JsonSchema"/>.</returns>
public static JsonSchema FromType<TType>(System.Text.Json.Serialization.JsonSerializerContext context)
{
    return FromType(typeof(TType), context);
}

/// <summary>Creates a <see cref="JsonSchema"/> from a given type using a source-generated JsonSerializerContext (AOT-compatible).</summary>
/// <param name="type">The type to generate the schema for.</param>
/// <param name="context">The JsonSerializerContext containing type metadata.</param>
/// <returns>The <see cref="JsonSchema"/>.</returns>
public static JsonSchema FromType(Type type, System.Text.Json.Serialization.JsonSerializerContext context)
{
    var settings = new SystemTextJsonSchemaGeneratorSettings
    {
        SerializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            TypeInfoResolver = context
        },
        FallbackToReflection = false
    };
    var generator = new JsonSchemaGenerator(settings);
    return generator.Generate(type);
}
#endif
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_FromType_with_JsonSerializerContext_then_schema_is_generated"`
Expected: FAIL initially — the `JsonTypeInfo` path doesn't exist yet. The test will pass once Task 6 is complete. For now, this test will use the reflection fallback... but `FallbackToReflection = false` means it will throw. **Temporarily set FallbackToReflection = true in the test** until Task 6 is done, then change it back.

**Step 5: Commit**

```bash
git add src/NJsonSchema/JsonSchema.cs src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs
git commit -m "feat: add FromType overloads accepting JsonSerializerContext for AOT"
```

---

## Phase 3: JsonTypeInfo Property Discovery (net8.0+)

### Task 6: Implement `TryGeneratePropertiesFromTypeInfo`

**Files:**
- Test: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTypeInfoTests.cs` (CREATE)
- Modify: `src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs`

**Step 1: Create test file with basic property discovery test**

Create `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTypeInfoTests.cs`:

```csharp
#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonTypeInfoTests
    {
        [Fact]
        public async Task When_using_TypeInfo_then_basic_properties_are_discovered()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert
            Assert.Contains("Name", schema.Properties.Keys);
            Assert.Contains("Age", schema.Properties.Keys);
            Assert.Equal(2, schema.Properties.Count);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_PropertyNamingPolicy_then_names_are_transformed()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                },
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert
            Assert.Contains("name", schema.Properties.Keys);
            Assert.Contains("age", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_JsonPropertyName_then_attribute_name_is_used()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoRenamedClass>(settings);

            // Assert
            Assert.Contains("full_name", schema.Properties.Keys);
            Assert.DoesNotContain("Name", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_JsonIgnore_then_property_is_excluded()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoIgnoredClass>(settings);

            // Assert
            Assert.Contains("Name", schema.Properties.Keys);
            Assert.DoesNotContain("Secret", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_JsonRequired_then_property_is_required()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoRequiredClass>(settings);

            // Assert
            Assert.Contains("Name", schema.RequiredProperties);
            Assert.DoesNotContain("Optional", schema.RequiredProperties);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_JsonPropertyOrder_then_properties_are_ordered()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoOrderedClass>(settings);

            // Assert
            var keys = schema.Properties.Keys.ToList();
            Assert.Equal("First", keys[0]);
            Assert.Equal("Second", keys[1]);
            Assert.Equal("Third", keys[2]);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_modifier_rename_then_modified_name_is_used()
        {
            // Arrange
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Type == typeof(TypeInfoBasicClass))
                {
                    foreach (var property in typeInfo.Properties)
                    {
                        if (property.Name == "Name")
                        {
                            property.Name = "modified_name";
                        }
                    }
                }
            });

            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions { TypeInfoResolver = resolver },
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert
            Assert.Contains("modified_name", schema.Properties.Keys);
            Assert.DoesNotContain("Name", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_modifier_ignore_then_property_is_excluded()
        {
            // Arrange
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Type == typeof(TypeInfoBasicClass))
                {
                    var nameProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "Name");
                    if (nameProp != null)
                    {
                        typeInfo.Properties.Remove(nameProp);
                    }
                }
            });

            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions { TypeInfoResolver = resolver },
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert
            Assert.DoesNotContain("Name", schema.Properties.Keys);
            Assert.Contains("Age", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_modifier_set_required_then_property_is_required()
        {
            // Arrange
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Type == typeof(TypeInfoBasicClass))
                {
                    foreach (var property in typeInfo.Properties)
                    {
                        if (property.Name == "Name")
                        {
                            property.IsRequired = true;
                        }
                    }
                }
            });

            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions { TypeInfoResolver = resolver },
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert
            Assert.Contains("Name", schema.RequiredProperties);
        }

        [Fact]
        public async Task When_FallbackToReflection_is_false_and_type_not_known_then_throws()
        {
            // Arrange
            var context = UnknownTypeTestJsonContext.Default;
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions { TypeInfoResolver = context },
                FallbackToReflection = false
            };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                JsonSchema.FromType<TypeInfoBasicClass>(settings));
        }

        [Fact]
        public async Task When_FallbackToReflection_is_true_and_type_not_known_then_uses_reflection()
        {
            // Arrange
            var context = UnknownTypeTestJsonContext.Default;
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions { TypeInfoResolver = context },
                FallbackToReflection = true
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoBasicClass>(settings);

            // Assert — reflection fallback discovers the properties
            Assert.Contains("Name", schema.Properties.Keys);
            Assert.Contains("Age", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_validation_attributes_then_constraints_are_applied()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoValidatedClass>(settings);

            // Assert
            var nameProperty = schema.Properties["Name"].ActualSchema;
            Assert.Equal(1, nameProperty.MinLength);
            Assert.Equal(100, nameProperty.MaxLength);

            var ageProperty = schema.Properties["Age"].ActualSchema;
            Assert.Equal(0, ageProperty.Minimum);
            Assert.Equal(150, ageProperty.Maximum);
        }

        [Fact]
        public async Task When_using_source_generated_context_then_schema_is_generated()
        {
            // Act
            var schema = JsonSchema.FromType<TypeInfoContextPerson>(TypeInfoTestJsonContext.Default);

            // Assert
            Assert.Contains("name", schema.Properties.Keys);
            Assert.Contains("age", schema.Properties.Keys);
        }

        [Fact]
        public async Task When_using_TypeInfo_with_string_enum_converter_modifier_then_enum_is_string()
        {
            // Arrange
            var resolver = new DefaultJsonTypeInfoResolver();
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions
                {
                    TypeInfoResolver = resolver,
                    Converters = { new JsonStringEnumConverter() }
                },
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoEnumClass>(settings);

            // Assert
            var statusProperty = schema.Properties["Status"];
            Assert.True(statusProperty.ActualSchema.Type.HasFlag(JsonObjectType.String));
        }

        [Fact]
        public async Task When_using_TypeInfo_with_extension_data_then_extension_property_is_excluded()
        {
            // Arrange
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = false
            };

            // Act
            var schema = JsonSchema.FromType<TypeInfoExtensionDataClass>(settings);

            // Assert
            Assert.Contains("Name", schema.Properties.Keys);
            Assert.DoesNotContain("ExtensionData", schema.Properties.Keys);
        }

        // --- Test model classes ---

        public class TypeInfoBasicClass
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
        }

        public class TypeInfoRenamedClass
        {
            [JsonPropertyName("full_name")]
            public string Name { get; set; } = "";
        }

        public class TypeInfoIgnoredClass
        {
            public string Name { get; set; } = "";

            [JsonIgnore]
            public string Secret { get; set; } = "";
        }

        public class TypeInfoRequiredClass
        {
            [JsonRequired]
            public string Name { get; set; } = "";
            public string? Optional { get; set; }
        }

        public class TypeInfoOrderedClass
        {
            [JsonPropertyOrder(3)]
            public string Third { get; set; } = "";

            [JsonPropertyOrder(1)]
            public string First { get; set; } = "";

            [JsonPropertyOrder(2)]
            public string Second { get; set; } = "";
        }

        public class TypeInfoValidatedClass
        {
            [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]
            public string Name { get; set; } = "";

            [System.ComponentModel.DataAnnotations.Range(0, 150)]
            public int Age { get; set; }
        }

        public class TypeInfoContextPerson
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("age")]
            public int Age { get; set; }
        }

        public enum TypeInfoStatus
        {
            Active,
            Inactive
        }

        public class TypeInfoEnumClass
        {
            public TypeInfoStatus Status { get; set; }
        }

        public class TypeInfoExtensionDataClass
        {
            public string Name { get; set; } = "";

            [JsonExtensionData]
            public Dictionary<string, object?>? ExtensionData { get; set; }
        }

        [JsonSerializable(typeof(TypeInfoContextPerson))]
        public partial class TypeInfoTestJsonContext : JsonSerializerContext
        {
        }

        [JsonSerializable(typeof(string))] // does NOT include TypeInfoBasicClass
        public partial class UnknownTypeTestJsonContext : JsonSerializerContext
        {
        }
    }
}
#endif
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "SystemTextJsonTypeInfoTests"`
Expected: Multiple failures — `TryGeneratePropertiesFromTypeInfo` doesn't exist yet.

**Step 3: Implement the `JsonTypeInfo` path**

In `SystemTextJsonReflectionService.cs`, refactor `GenerateProperties` and add the new method:

```csharp
using System.Linq;
using Namotion.Reflection;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace NJsonSchema.Generation
{
    public class SystemTextJsonReflectionService : ReflectionServiceBase<SystemTextJsonSchemaGeneratorSettings>
    {
        /// <inheritdoc />
        public override void GenerateProperties(JsonSchema schema, ContextualType contextualType,
            SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator,
            JsonSchemaResolver schemaResolver)
        {
#if NET8_0_OR_GREATER
            if (TryGeneratePropertiesFromTypeInfo(schema, contextualType, settings, schemaGenerator, schemaResolver))
            {
                return;
            }

            if (!settings.FallbackToReflection)
            {
                throw new NotSupportedException(
                    $"Type '{contextualType.Type.FullName}' could not be resolved via JsonSerializerOptions. " +
                    "Configure a TypeInfoResolver or set FallbackToReflection = true.");
            }
#endif

            GeneratePropertiesFromReflection(schema, contextualType, settings, schemaGenerator, schemaResolver);
        }

#if NET8_0_OR_GREATER
        private bool TryGeneratePropertiesFromTypeInfo(JsonSchema schema, ContextualType contextualType,
            SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator,
            JsonSchemaResolver schemaResolver)
        {
            JsonTypeInfo typeInfo;
            try
            {
                typeInfo = settings.SerializerOptions.GetTypeInfo(contextualType.Type);
            }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
            {
                return false;
            }

            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return false;
            }

            foreach (var jsonProperty in typeInfo.Properties)
            {
                if (jsonProperty.IsExtensionData)
                {
                    continue;
                }

                // ShouldSerialize being non-null with a delegate that always returns false means the property is ignored.
                // However, we can't easily evaluate the delegate without an instance.
                // The Get accessor being null means the property is not readable (write-only), skip it.
                if (jsonProperty.Get == null && jsonProperty.Set == null)
                {
                    continue;
                }

                var propertyName = jsonProperty.Name;
                var propertyType = jsonProperty.PropertyType;

                // Check if excluded by settings
                if (Array.IndexOf(settings.ExcludedTypeNames, propertyType.FullName) != -1)
                {
                    continue;
                }

                // Bridge to CLR member for NJsonSchema-specific attributes
                var accessorInfo = FindAccessorForJsonProperty(contextualType, jsonProperty);

                if (accessorInfo != null && schemaGenerator.IsPropertyIgnoredBySettings(accessorInfo))
                {
                    continue;
                }

                // Handle duplicate property names (inheritance flattening)
                if (schema.Properties.ContainsKey(propertyName))
                {
                    if (settings.GetActualFlattenInheritanceHierarchy(contextualType.Type))
                    {
                        schema.Properties.Remove(propertyName);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"The JSON property '{propertyName}' is defined multiple times on type '{contextualType.Type.FullName}'.");
                    }
                }

                // Required: from JsonPropertyInfo or from CLR attributes
                var isRequired = jsonProperty.IsRequired;
                if (!isRequired && accessorInfo != null)
                {
                    var attributes = accessorInfo.GetAttributes(true).ToArray();
                    var requiredAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");
                    var isDataContractMemberRequired = schemaGenerator.GetDataMemberAttribute(accessorInfo, contextualType.Type)?.IsRequired == true;
                    isRequired = requiredAttribute != null || isDataContractMemberRequired;
                }

                if (isRequired)
                {
                    schema.RequiredProperties.Add(propertyName);
                }

                // Build type description
                var contextualPropertyType = accessorInfo != null
                    ? accessorInfo.AccessorType
                    : propertyType.ToContextualType();

                var propertyTypeDescription = GetDescription(
                    contextualPropertyType,
                    settings.DefaultReferenceTypeNullHandling,
                    settings);

                var isNullable = propertyTypeDescription.IsNullable && !isRequired;

                // Get requiredAttribute for AddProperty (needed for validation attributes)
                object? requiredAttributeForProperty = null;
                if (accessorInfo != null)
                {
                    requiredAttributeForProperty = accessorInfo.GetAttributes(true)
                        .FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.RequiredAttribute");
                }

                schemaGenerator.AddProperty(
                    schema, accessorInfo, propertyTypeDescription,
                    propertyName, requiredAttributeForProperty, isRequired,
                    isNullable, null, schemaResolver);
            }

            return true;
        }

        private static ContextualAccessorInfo? FindAccessorForJsonProperty(
            ContextualType contextualType, JsonPropertyInfo jsonProperty)
        {
            if (jsonProperty.AttributeProvider is MemberInfo memberInfo)
            {
                return contextualType.Properties
                    .OfType<ContextualAccessorInfo>()
                    .Concat(contextualType.Fields)
                    .FirstOrDefault(a => a.MemberInfo == memberInfo);
            }

            // Synthetic property from modifier — no CLR member available
            return null;
        }
#endif

        private void GeneratePropertiesFromReflection(JsonSchema schema, ContextualType contextualType,
            SystemTextJsonSchemaGeneratorSettings settings, JsonSchemaGenerator schemaGenerator,
            JsonSchemaResolver schemaResolver)
        {
            // --- existing GenerateProperties body goes here (lines 25-113 of current file) ---
            foreach (var accessorInfo in contextualType.Properties.OfType<ContextualAccessorInfo>().Concat(contextualType.Fields)
                .OrderBy(a => GetPropertyOrder(a)))
            {
                // ... entire existing implementation unchanged ...
            }
        }

        // ... rest of existing methods unchanged ...
    }
}
```

**Step 4: Run all TypeInfo tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "SystemTextJsonTypeInfoTests"`
Expected: All pass.

**Step 5: Run all existing tests for regressions**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj`
Expected: All 520+ tests pass.

**Step 6: Commit**

```bash
git add src/NJsonSchema/Generation/SystemTextJsonReflectionService.cs src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTypeInfoTests.cs
git commit -m "feat: implement JsonTypeInfo-based property discovery for net8.0+"
```

---

### Task 7: Update the `FromType(JsonSerializerContext)` test to use strict mode

**Files:**
- Modify: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs`

Now that `TryGeneratePropertiesFromTypeInfo` exists, update the Task 5 test to use `FallbackToReflection = false` if it was temporarily set to true.

**Step 1: Verify the test passes with strict mode**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "When_FromType_with_JsonSerializerContext_then_schema_is_generated"`
Expected: PASS

**Step 2: Commit if any changes**

```bash
git add src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonTests.cs
git commit -m "test: verify FromType(JsonSerializerContext) works in strict AOT mode"
```

---

## Phase 4: Shared Test Coverage (Both Paths)

### Task 8: Add parameterized tests that verify both paths produce identical results

**Files:**
- Create: `src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonDualPathTests.cs`

**Step 1: Write the parameterized tests**

```csharp
#if NET8_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation.SystemTextJson
{
    public class SystemTextJsonDualPathTests
    {
        private static JsonSchema GenerateSchema<T>(bool useJsonTypeInfo)
        {
            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General),
                FallbackToReflection = !useJsonTypeInfo
            };

            if (useJsonTypeInfo)
            {
                // Ensure DefaultJsonTypeInfoResolver is set for TypeInfo path
                settings.SerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            }

            return JsonSchema.FromType(typeof(T), settings);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_discover_basic_properties(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualBasicClass>(useJsonTypeInfo);

            Assert.Contains("Name", schema.Properties.Keys);
            Assert.Contains("Age", schema.Properties.Keys);
            Assert.Equal(2, schema.Properties.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_respect_JsonPropertyName(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualRenamedClass>(useJsonTypeInfo);

            Assert.Contains("full_name", schema.Properties.Keys);
            Assert.DoesNotContain("Name", schema.Properties.Keys);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_respect_JsonIgnore(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualIgnoredClass>(useJsonTypeInfo);

            Assert.Contains("Name", schema.Properties.Keys);
            Assert.DoesNotContain("Secret", schema.Properties.Keys);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_respect_JsonRequired(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualRequiredClass>(useJsonTypeInfo);

            Assert.Contains("Name", schema.RequiredProperties);
            Assert.DoesNotContain("Optional", schema.RequiredProperties);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_respect_JsonPropertyOrder(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualOrderedClass>(useJsonTypeInfo);

            var keys = schema.Properties.Keys.ToList();
            Assert.Equal("First", keys[0]);
            Assert.Equal("Second", keys[1]);
            Assert.Equal("Third", keys[2]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_apply_validation_attributes(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualValidatedClass>(useJsonTypeInfo);

            var nameProperty = schema.Properties["Name"].ActualSchema;
            Assert.Equal(1, nameProperty.MinLength);
            Assert.Equal(100, nameProperty.MaxLength);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_exclude_extension_data(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualExtensionDataClass>(useJsonTypeInfo);

            Assert.Contains("Name", schema.Properties.Keys);
            Assert.DoesNotContain("ExtensionData", schema.Properties.Keys);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Both_paths_handle_nullable_properties(bool useJsonTypeInfo)
        {
            var schema = GenerateSchema<DualNullableClass>(useJsonTypeInfo);

            var nullableProp = schema.Properties["NullableValue"];
            Assert.True(nullableProp.IsNullableRaw == true || nullableProp.Type.HasFlag(JsonObjectType.Null));
        }

        // --- Test model classes ---

        public class DualBasicClass
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
        }

        public class DualRenamedClass
        {
            [JsonPropertyName("full_name")]
            public string Name { get; set; } = "";
        }

        public class DualIgnoredClass
        {
            public string Name { get; set; } = "";
            [JsonIgnore]
            public string Secret { get; set; } = "";
        }

        public class DualRequiredClass
        {
            [JsonRequired]
            public string Name { get; set; } = "";
            public string? Optional { get; set; }
        }

        public class DualOrderedClass
        {
            [JsonPropertyOrder(3)]
            public string Third { get; set; } = "";
            [JsonPropertyOrder(1)]
            public string First { get; set; } = "";
            [JsonPropertyOrder(2)]
            public string Second { get; set; } = "";
        }

        public class DualValidatedClass
        {
            [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]
            public string Name { get; set; } = "";
        }

        public class DualExtensionDataClass
        {
            public string Name { get; set; } = "";
            [JsonExtensionData]
            public Dictionary<string, object?>? ExtensionData { get; set; }
        }

        public class DualNullableClass
        {
            public string NonNullableValue { get; set; } = "";
            public string? NullableValue { get; set; }
        }
    }
}
#endif
```

**Step 2: Run all dual-path tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "SystemTextJsonDualPathTests"`
Expected: All pass — both paths produce equivalent schemas.

**Step 3: Commit**

```bash
git add src/NJsonSchema.Tests/Generation/SystemTextJson/SystemTextJsonDualPathTests.cs
git commit -m "test: add parameterized dual-path tests verifying TypeInfo and reflection equivalence"
```

---

## Phase 5: Final Verification

### Task 9: Full test suite run and cleanup

**Step 1: Run the entire test suite**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj`
Expected: All tests pass.

**Step 2: Run the full build including pack**

Run: `./build.sh Compile Test Pack`
Expected: Build, test, and pack all succeed.

**Step 3: Final commit if any cleanup needed**

```bash
git add -A
git commit -m "chore: cleanup after JsonTypeInfo schema generation implementation"
```
