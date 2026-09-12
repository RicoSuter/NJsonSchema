# Migrate NJsonSchema Core to System.Text.Json — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove Newtonsoft.Json from all NJsonSchema projects except NJsonSchema.NewtonsoftJson, replacing it with System.Text.Json throughout.

**Architecture:** The core library uses Newtonsoft.Json in 5 areas: model attributes, serialization infrastructure, validation (JToken), sample generation (JToken), and contract resolvers. Each area is migrated in phases. A custom `SchemaSerializationConverter` replaces Newtonsoft's `ContractResolver` pattern for property renaming/ignoring per SchemaType. `JsonNode` replaces `JToken` for validation and sample generation. Multi-targeting (`netstandard2.0`, `net462`, `net8.0`) is preserved — all STJ APIs used are available on all targets via the System.Text.Json NuGet package.

**Tech Stack:** C#, System.Text.Json, XUnit v3, Verify (snapshot testing)

**Design doc:** `docs/plans/2026-03-08-migrate-core-to-system-text-json.md`

---

## Phase 0: Test Hardening

Before any migration, add tests to lock down current behavior so regressions are caught during migration.

### Task 0.1: Schema serialization round-trip tests

**Files:**
- Create: `src/NJsonSchema.Tests/Serialization/SchemaSerializationRoundTripTests.cs`

**Step 1: Write round-trip tests for all SchemaTypes**

```csharp
using NJsonSchema.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NJsonSchema.Tests.Serialization;

public class SchemaSerializationRoundTripTests
{
    [Fact]
    public async Task RoundTrip_SimpleSchema_JsonSchemaType()
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Title = "Test",
            Description = "A test schema"
        };
        schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            MinLength = 1,
            MaxLength = 100
        };
        schema.Properties["age"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Integer,
            Minimum = 0,
            Maximum = 150
        };
        schema.RequiredProperties.Add("name");

        // Act
        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        // Assert
        Assert.Equal(json, json2);
        Assert.Equal("Test", deserialized.Title);
        Assert.Equal(JsonObjectType.String, deserialized.Properties["name"].Type);
        Assert.Contains("name", deserialized.RequiredProperties);
    }

    [Theory]
    [InlineData(SchemaType.JsonSchema)]
    [InlineData(SchemaType.OpenApi3)]
    [InlineData(SchemaType.Swagger2)]
    public void RoundTrip_SchemaWithNullable_PerSchemaType(SchemaType schemaType)
    {
        // Arrange
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };
        schema.Properties["value"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String | JsonObjectType.Null
        };

        // Act
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(schemaType);
        var json = JsonSchemaSerialization.ToJson(schema, schemaType, resolver, Formatting.Indented);
        var deserialized = JsonSchemaSerialization.FromJson<JsonSchema>(json, resolver);

        // Assert
        Assert.NotNull(deserialized);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithAllOf()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.AllOf.Add(new JsonSchema
        {
            Type = JsonObjectType.Object,
        });
        schema.AllOf[0].Properties["id"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        Assert.Equal(json, json2);
        Assert.Single(deserialized.AllOf);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithOneOfAndAnyOf()
    {
        var schema = new JsonSchema();
        schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.String });
        schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.Integer });
        schema.AnyOf.Add(new JsonSchema { Type = JsonObjectType.Number });

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        Assert.Equal(json, json2);
        Assert.Equal(2, deserialized.OneOf.Count);
        Assert.Single(deserialized.AnyOf);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithEnumeration()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String };
        schema.Enumeration.Add("red");
        schema.Enumeration.Add("green");
        schema.Enumeration.Add("blue");
        schema.EnumerationNames.Add("Red");
        schema.EnumerationNames.Add("Green");
        schema.EnumerationNames.Add("Blue");

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        Assert.Equal(json, json2);
        Assert.Equal(3, deserialized.Enumeration.Count);
        Assert.Equal(3, deserialized.EnumerationNames.Count);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithDefinitionsAndReferences()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        var addressSchema = new JsonSchema { Type = JsonObjectType.Object };
        addressSchema.Properties["street"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Definitions["Address"] = addressSchema;
        schema.Properties["address"] = new JsonSchemaProperty();
        schema.Properties["address"].Reference = addressSchema;

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        Assert.Equal(json, json2);
        Assert.NotNull(deserialized.Properties["address"].Reference);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithExclusiveMinMax()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Number,
            ExclusiveMinimum = 0,
            ExclusiveMaximum = 100
        };

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);

        Assert.Equal(0, deserialized.ExclusiveMinimum);
        Assert.Equal(100, deserialized.ExclusiveMaximum);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithAdditionalProperties()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = false
        };

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);

        Assert.False(deserialized.AllowAdditionalProperties);
    }

    [Fact]
    public async Task RoundTrip_SchemaWithPatternProperties()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.PatternProperties["^x-"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        var json = schema.ToJson();
        var deserialized = await JsonSchema.FromJsonAsync(json);
        var json2 = deserialized.ToJson();

        Assert.Equal(json, json2);
        Assert.Single(deserialized.PatternProperties);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SchemaSerializationRoundTripTests" -v n`
Expected: ALL PASS

**Step 3: Commit**

```bash
git add src/NJsonSchema.Tests/Serialization/SchemaSerializationRoundTripTests.cs
git commit -m "test: add schema serialization round-trip tests for STJ migration"
```

---

### Task 0.2: Property rename/ignore behavior tests per SchemaType

**Files:**
- Create: `src/NJsonSchema.Tests/Serialization/SchemaTypePropertyBehaviorTests.cs`

**Step 1: Write tests that verify property renaming per SchemaType**

```csharp
using NJsonSchema.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NJsonSchema.Tests.Serialization;

public class SchemaTypePropertyBehaviorTests
{
    [Fact]
    public void OpenApi3_RenamesNullableProperty()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["value"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, Formatting.Indented);

        // x-nullable should be serialized as "nullable" in OpenApi3
        Assert.DoesNotContain("x-nullable", json);
    }

    [Fact]
    public void OpenApi3_RenamesReadOnlyProperty()
    {
        var property = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        // Set via the extension data approach
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["test"] = property;

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, Formatting.Indented);

        // Should not contain x-readOnly, x-writeOnly in output
        Assert.DoesNotContain("x-readOnly", json);
        Assert.DoesNotContain("x-writeOnly", json);
    }

    [Fact]
    public void Swagger2_AdditionalProperties_EmptyObjectWhenAllowed()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.Swagger2);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, Formatting.Indented);

        Assert.Contains("\"additionalProperties\": {}", json);
    }

    [Fact]
    public void JsonSchemaType_AdditionalProperties_OmittedWhenAllowed()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.JsonSchema);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.JsonSchema, resolver, Formatting.Indented);

        Assert.DoesNotContain("additionalProperties", json);
    }

    [Fact]
    public void EmptyCollections_AreNotSerialized()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.String
        };
        // Properties, AllOf, OneOf, AnyOf are empty — should not appear in JSON

        var json = schema.ToJson();

        Assert.DoesNotContain("\"properties\"", json);
        Assert.DoesNotContain("\"allOf\"", json);
        Assert.DoesNotContain("\"oneOf\"", json);
        Assert.DoesNotContain("\"anyOf\"", json);
        Assert.DoesNotContain("\"definitions\"", json);
        Assert.DoesNotContain("\"required\"", json);
        Assert.DoesNotContain("\"enum\"", json);
    }

    [Fact]
    public async Task ExtensionData_SurvivesRoundTrip()
    {
        var json = """
        {
            "type": "object",
            "x-custom-tag": "hello",
            "x-custom-number": 42,
            "x-custom-object": { "nested": true }
        }
        """;

        var schema = await JsonSchema.FromJsonAsync(json);
        var output = schema.ToJson();

        Assert.Contains("x-custom-tag", output);
        Assert.Contains("x-custom-number", output);
        Assert.Contains("x-custom-object", output);
    }

    [Fact]
    public async Task ExtensionData_SchemasAreDeserialized()
    {
        var json = """
        {
            "type": "object",
            "x-schema": {
                "type": "string",
                "properties": {
                    "name": { "type": "string" }
                }
            }
        }
        """;

        var schema = await JsonSchema.FromJsonAsync(json);

        // Extension data containing schema-like objects should be deserialized as JsonSchema
        Assert.NotNull(schema.ExtensionData);
        Assert.True(schema.ExtensionData!.ContainsKey("x-schema"));
        Assert.IsType<JsonSchema>(schema.ExtensionData["x-schema"]);
    }

    [Fact]
    public async Task Discriminator_Swagger2_SerializedAsString()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Discriminator = "type";

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.Swagger2);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, Formatting.Indented);

        Assert.Contains("\"discriminator\": \"type\"", json);
    }

    [Fact]
    public async Task Discriminator_OpenApi3_SerializedAsObject()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.DiscriminatorObject = new OpenApiDiscriminator
        {
            PropertyName = "type"
        };

        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, Formatting.Indented);

        Assert.Contains("\"propertyName\"", json);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SchemaTypePropertyBehaviorTests" -v n`
Expected: ALL PASS

**Step 3: Commit**

```bash
git add src/NJsonSchema.Tests/Serialization/SchemaTypePropertyBehaviorTests.cs
git commit -m "test: add SchemaType property rename/ignore behavior tests"
```

---

### Task 0.3: Snapshot serialization output per SchemaType

**Files:**
- Create: `src/NJsonSchema.Tests/Serialization/SchemaSerializationSnapshotTests.cs`

**Step 1: Write snapshot tests for complex schemas per SchemaType**

```csharp
using NJsonSchema.Infrastructure;
using Newtonsoft.Json;

namespace NJsonSchema.Tests.Serialization;

public class SchemaSerializationSnapshotTests
{
    private static JsonSchema CreateComplexSchema()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Title = "Person",
            Description = "A person object"
        };

        schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            MinLength = 1,
            MaxLength = 200,
            Pattern = "^[a-zA-Z ]+$"
        };

        schema.Properties["age"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Integer,
            Minimum = 0,
            Maximum = 150
        };

        schema.Properties["email"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Format = "email"
        };

        schema.RequiredProperties.Add("name");
        schema.RequiredProperties.Add("age");

        var addressSchema = new JsonSchema { Type = JsonObjectType.Object };
        addressSchema.Properties["street"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        addressSchema.Properties["city"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Definitions["Address"] = addressSchema;

        schema.Properties["address"] = new JsonSchemaProperty();
        schema.Properties["address"].Reference = addressSchema;

        schema.Properties["tags"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };

        schema.Enumeration.Add("active");
        schema.Enumeration.Add("inactive");

        return schema;
    }

    [Fact]
    public Task Snapshot_ComplexSchema_JsonSchemaType()
    {
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.JsonSchema);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.JsonSchema, resolver, Formatting.Indented);
        return VerifyHelper.Verify(json);
    }

    [Fact]
    public Task Snapshot_ComplexSchema_OpenApi3()
    {
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.OpenApi3);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.OpenApi3, resolver, Formatting.Indented);
        return VerifyHelper.Verify(json);
    }

    [Fact]
    public Task Snapshot_ComplexSchema_Swagger2()
    {
        var schema = CreateComplexSchema();
        var resolver = JsonSchema.CreateJsonSerializerContractResolver(SchemaType.Swagger2);
        var json = JsonSchemaSerialization.ToJson(schema, SchemaType.Swagger2, resolver, Formatting.Indented);
        return VerifyHelper.Verify(json);
    }
}
```

**Step 2: Run tests to generate initial snapshots**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SchemaSerializationSnapshotTests" -v n`
Expected: FAIL (snapshots don't exist yet)

**Step 3: Accept the generated snapshots**

Review the `.received.txt` files in `src/NJsonSchema.Tests/Serialization/Snapshots/`, rename them to `.verified.txt`.

**Step 4: Re-run tests to verify they pass**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SchemaSerializationSnapshotTests" -v n`
Expected: ALL PASS

**Step 5: Commit**

```bash
git add src/NJsonSchema.Tests/Serialization/SchemaSerializationSnapshotTests.cs
git add src/NJsonSchema.Tests/Serialization/Snapshots/
git commit -m "test: add snapshot tests for schema serialization per SchemaType"
```

---

### Task 0.4: Validation system tests with explicit JToken construction patterns

**Files:**
- Create: `src/NJsonSchema.Tests/Validation/ValidationRegressionTests.cs`

**Step 1: Write tests covering all JToken type paths in the validator**

These tests verify the exact validation behavior that must be preserved when migrating from JToken to JsonNode.

```csharp
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Tests.Validation;

public class ValidationRegressionTests
{
    [Fact]
    public void Validate_StringFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String };
        var errors = schema.Validate("\"hello\"");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_IntegerFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Integer };
        var errors = schema.Validate("42");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NumberFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Number };
        var errors = schema.Validate("3.14");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BooleanFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Boolean };
        var errors = schema.Validate("true");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NullFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Null };
        var errors = schema.Validate("null");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ArrayFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.Integer }
        };
        var errors = schema.Validate("[1, 2, 3]");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ObjectFromJson_ReturnsNoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        var errors = schema.Validate("{\"name\": \"test\"}");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WrongType_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String };
        var errors = schema.Validate("42");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_MissingRequired_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.RequiredProperties.Add("name");
        var errors = schema.Validate("{}");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.PropertyRequired);
    }

    [Fact]
    public void Validate_MinLength_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String, MinLength = 5 };
        var errors = schema.Validate("\"ab\"");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Kind == ValidationErrorKind.StringTooShort);
    }

    [Fact]
    public void Validate_Pattern_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String, Pattern = "^[0-9]+$" };
        var errors = schema.Validate("\"abc\"");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Minimum_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Integer, Minimum = 10 };
        var errors = schema.Validate("5");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_NestedObject_ReturnsPathInErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["address"] = new JsonSchemaProperty { Type = JsonObjectType.Object };
        schema.Properties["address"].Properties["zip"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["address"].RequiredProperties.Add("zip");

        var errors = schema.Validate("{\"address\": {}}");
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Path != null && e.Path.Contains("address"));
    }

    [Fact]
    public void Validate_FormatEmail_ReturnsErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "email" };
        var errors = schema.Validate("\"not-an-email\"");
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_FormatDateTime_NoErrors()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "date-time" };
        var errors = schema.Validate("\"2024-01-15T10:30:00Z\"");
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UniqueItems_ReturnsErrors()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Array,
            UniqueItems = true,
            Item = new JsonSchema { Type = JsonObjectType.Integer }
        };
        var errors = schema.Validate("[1, 2, 1]");
        Assert.NotEmpty(errors);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~ValidationRegressionTests" -v n`
Expected: ALL PASS

**Step 3: Commit**

```bash
git add src/NJsonSchema.Tests/Validation/ValidationRegressionTests.cs
git commit -m "test: add validation regression tests for STJ migration"
```

---

### Task 0.5: Sample data generation regression tests

**Files:**
- Create: `src/NJsonSchema.Tests/Generation/SampleJsonDataGeneratorRegressionTests.cs`

**Step 1: Write tests covering sample data generation output**

```csharp
using Newtonsoft.Json.Linq;

namespace NJsonSchema.Tests.Generation;

public class SampleJsonDataGeneratorRegressionTests
{
    [Fact]
    public void Generate_StringProperty_ReturnsString()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        Assert.IsType<JObject>(token);
        Assert.NotNull(((JObject)token)["name"]);
        Assert.Equal(JTokenType.String, ((JObject)token)["name"]!.Type);
    }

    [Fact]
    public void Generate_IntegerProperty_ReturnsInteger()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["count"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        Assert.Equal(JTokenType.Integer, ((JObject)token)["count"]!.Type);
    }

    [Fact]
    public void Generate_ArrayProperty_ReturnsArray()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["items"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema { Type = JsonObjectType.String }
        };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        Assert.Equal(JTokenType.Array, ((JObject)token)["items"]!.Type);
    }

    [Fact]
    public void Generate_NestedObject_ReturnsNestedObject()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["address"] = new JsonSchemaProperty { Type = JsonObjectType.Object };
        schema.Properties["address"].Properties["city"] = new JsonSchemaProperty { Type = JsonObjectType.String };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        var address = ((JObject)token)["address"];
        Assert.NotNull(address);
        Assert.Equal(JTokenType.Object, address!.Type);
    }

    [Fact]
    public void Generate_EnumProperty_ReturnsEnumValue()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        var prop = new JsonSchemaProperty { Type = JsonObjectType.String };
        prop.Enumeration.Add("red");
        prop.Enumeration.Add("green");
        prop.Enumeration.Add("blue");
        schema.Properties["color"] = prop;

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        var color = ((JObject)token)["color"]?.Value<string>();
        Assert.Contains(color, new[] { "red", "green", "blue" });
    }

    [Fact]
    public void Generate_WithDefaultValue_UsesDefault()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["status"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Default = "active"
        };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        Assert.Equal("active", ((JObject)token)["status"]?.Value<string>());
    }

    [Fact]
    public void Generate_OutputIsValidJson()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["name"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["age"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        schema.Properties["active"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean };

        var generator = new SampleJsonDataGenerator();
        var token = generator.Generate(schema);

        var jsonString = token.ToString();
        Assert.NotEmpty(jsonString);
        // Verify it's valid JSON by parsing
        var reparsed = JToken.Parse(jsonString);
        Assert.NotNull(reparsed);
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SampleJsonDataGeneratorRegressionTests" -v n`
Expected: ALL PASS

**Step 3: Commit**

```bash
git add src/NJsonSchema.Tests/Generation/SampleJsonDataGeneratorRegressionTests.cs
git commit -m "test: add sample data generation regression tests for STJ migration"
```

---

### Task 0.6: Run full test suite baseline

**Step 1: Run the full test suite and record baseline**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -v n`
Expected: ALL PASS — record the total test count

**Step 2: Run CodeGen tests too**

Run: `dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj -v n`
Run: `dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -v n`
Expected: ALL PASS

---

## Phase 1: Schema Model & Serialization Infrastructure

This phase replaces the Newtonsoft serialization engine with STJ. This is the largest and most complex phase.

### Task 1.1: Create SchemaSerializationConverter

This replaces both `PropertyRenameAndIgnoreSerializerContractResolver` and `IgnoreEmptyCollectionsContractResolver`.

**Files:**
- Create: `src/NJsonSchema/Infrastructure/SchemaSerializationConverter.cs`

**Step 1: Write the converter**

```csharp
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NJsonSchema.Infrastructure;

/// <summary>
/// Custom STJ converter that supports property renaming, ignoring,
/// and empty collection skipping — replacing Newtonsoft's ContractResolver pattern.
/// </summary>
public class SchemaSerializationConverter : JsonConverter<object>
{
    private readonly Dictionary<string, HashSet<string>> _ignores = [];
    private readonly Dictionary<string, Dictionary<string, string>> _renames = [];
    private readonly Dictionary<string, Dictionary<string, string>> _reverseRenames = [];

    /// <summary>Ignore the given property/properties of the given type.</summary>
    public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
    {
        if (!_ignores.TryGetValue(type.FullName!, out var value))
        {
            value = [];
            _ignores[type.FullName!] = value;
        }

        foreach (var prop in jsonPropertyNames)
        {
            value.Add(prop);
        }
    }

    /// <summary>Rename a property of the given type.</summary>
    public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
    {
        if (!_renames.TryGetValue(type.FullName!, out var value))
        {
            value = [];
            _renames[type.FullName!] = value;
        }
        value[propertyName] = newJsonPropertyName;

        if (!_reverseRenames.TryGetValue(type.FullName!, out var reverse))
        {
            reverse = [];
            _reverseRenames[type.FullName!] = reverse;
        }
        reverse[newJsonPropertyName] = propertyName;
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(JsonExtensionObject).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc/>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialization is handled by STJ's default behavior with attributes.
        // This converter is primarily for write-time filtering/renaming.
        // We create a copy of options without this converter to avoid infinite recursion.
        var newOptions = CreateOptionsWithoutSelf(options);
        return JsonSerializer.Deserialize(ref reader, typeToConvert, newOptions);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        // Serialize without this converter to get the raw JSON, then filter/rename
        var newOptions = CreateOptionsWithoutSelf(options);
        using var doc = JsonSerializer.SerializeToDocument(value, value.GetType(), newOptions);
        var root = doc.RootElement;

        writer.WriteStartObject();
        foreach (var prop in root.EnumerateObject())
        {
            var propName = prop.Name;
            var declaringType = value.GetType();

            // Check ignores
            if (IsIgnored(declaringType, propName))
                continue;

            // Check empty collection skipping
            if (IsEmptyCollection(prop.Value))
                continue;

            // Apply renames
            var outputName = GetRenamedPropertyName(declaringType, propName);

            writer.WritePropertyName(outputName);
            prop.Value.WriteTo(writer);
        }
        writer.WriteEndObject();
    }

    private bool IsIgnored(Type type, string jsonPropertyName)
    {
        var current = type;
        while (current != null)
        {
            if (_ignores.TryGetValue(current.FullName!, out var ignored) && ignored.Contains(jsonPropertyName))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private string GetRenamedPropertyName(Type type, string jsonPropertyName)
    {
        var current = type;
        while (current != null)
        {
            if (_renames.TryGetValue(current.FullName!, out var renames) && renames.TryGetValue(jsonPropertyName, out var newName))
                return newName;
            current = current.BaseType;
        }
        return jsonPropertyName;
    }

    private static bool IsEmptyCollection(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 0)
            return true;
        if (element.ValueKind == JsonValueKind.Object && !element.EnumerateObject().Any())
            return true;
        return false;
    }

    private JsonSerializerOptions CreateOptionsWithoutSelf(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        // Remove this converter to prevent infinite recursion
        for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (newOptions.Converters[i] is SchemaSerializationConverter)
            {
                newOptions.Converters.RemoveAt(i);
            }
        }
        return newOptions;
    }
}
```

> **Note:** This is a starting point. The exact implementation will need refinement during migration as we discover edge cases with the `*Raw` properties and the recursive schema serialization. The empty collection logic may need to distinguish between "empty because no items" vs "intentionally empty object `{}`" (Swagger2 `additionalProperties`).

**Step 2: Run build to verify compilation**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`
Expected: BUILD SUCCEEDED

**Step 3: Commit**

```bash
git add src/NJsonSchema/Infrastructure/SchemaSerializationConverter.cs
git commit -m "feat: add SchemaSerializationConverter replacing Newtonsoft ContractResolvers"
```

---

### Task 1.2: Swap attributes on model classes — simple [JsonIgnore] replacements

These files only use `[JsonIgnore]` from Newtonsoft. The STJ `[JsonIgnore]` is a drop-in replacement.

**Files to modify** (swap `using Newtonsoft.Json;` → `using System.Text.Json.Serialization;`):
- `src/NJsonSchema/JsonSchemaProperty.cs` (lines 10, 20, 26)
- `src/NJsonSchema/JsonSchema.Reference.cs` (lines 10, 20, 33, 49)
- `src/NJsonSchema/IDocumentPathProvider.cs` (lines 9, 17)
- `src/NJsonSchema/References/IJsonReference.cs` (lines 9, 18, 22)

**Step 1: Update each file**

For each file listed above:
1. Replace `using Newtonsoft.Json;` with `using System.Text.Json.Serialization;`
2. The `[JsonIgnore]` attribute has the same name in STJ, so no attribute changes needed

**Files with `[JsonIgnore]` but also other Newtonsoft usage** (handle these LATER in subsequent tasks):
- `src/NJsonSchema/JsonSchema.Serialization.cs` — has both `[JsonIgnore]` and `[JsonProperty]` and `[JsonConverter]`
- `src/NJsonSchema/OpenApiDiscriminator.cs` — has `[JsonIgnore]`, `[JsonProperty]`, and `[JsonConverter]`
- `src/NJsonSchema/References/IJsonReferenceBase.cs` — has both `[JsonIgnore]` and `[JsonProperty]`
- `src/NJsonSchema/References/JsonReferenceBase.cs` — has both `[JsonIgnore]` and `[JsonProperty]`

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`
Expected: BUILD SUCCEEDED

**Step 3: Commit**

```bash
git add src/NJsonSchema/JsonSchemaProperty.cs src/NJsonSchema/JsonSchema.Reference.cs src/NJsonSchema/IDocumentPathProvider.cs src/NJsonSchema/References/IJsonReference.cs
git commit -m "refactor: swap Newtonsoft JsonIgnore to STJ on simple model classes"
```

---

### Task 1.3: Swap attributes on settings classes

**Files to modify:**
- `src/NJsonSchema/Generation/JsonSchemaGeneratorSettings.cs` — lines 12, 95, 99, 103, 107, 111: `[JsonIgnore]` on settings properties
- `src/NJsonSchema/Generation/SystemTextJsonSchemaGeneratorSettings.cs` — lines 9, 27: `[JsonIgnore]` on SerializerOptions

**Step 1: Update each file**

Replace `using Newtonsoft.Json;` with `using System.Text.Json.Serialization;` in both files.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`
Expected: BUILD SUCCEEDED

**Step 3: Commit**

```bash
git add src/NJsonSchema/Generation/JsonSchemaGeneratorSettings.cs src/NJsonSchema/Generation/SystemTextJsonSchemaGeneratorSettings.cs
git commit -m "refactor: swap Newtonsoft JsonIgnore to STJ on settings classes"
```

---

### Task 1.4: Swap attributes on JsonXmlObject

**File:** `src/NJsonSchema/JsonXmlObject.cs`

This file uses `[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]` which needs to be replaced with STJ equivalents.

**Step 1: Update the file**

Replace:
```csharp
using Newtonsoft.Json;
```
With:
```csharp
using System.Text.Json.Serialization;
```

Replace `[JsonIgnore]` (stays same name).

Replace `[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]` patterns with:
```csharp
[JsonPropertyName("name")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
```

Apply this pattern to properties: `Name`, `Wrapped`, `Namespace`.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/JsonXmlObject.cs
git commit -m "refactor: swap Newtonsoft attributes to STJ on JsonXmlObject"
```

---

### Task 1.5: Swap attributes on reference base classes

**Files:**
- `src/NJsonSchema/References/IJsonReferenceBase.cs` — `[JsonProperty("$ref", ...)]` and `[JsonIgnore]`
- `src/NJsonSchema/References/JsonReferenceBase.cs` — `[JsonProperty("$ref", ...)]` and `[JsonIgnore]`

**Step 1: Update each file**

Replace `using Newtonsoft.Json;` with `using System.Text.Json.Serialization;`.

Replace `[JsonProperty("$ref", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]` with:
```csharp
[JsonPropertyName("$ref")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
```

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/References/IJsonReferenceBase.cs src/NJsonSchema/References/JsonReferenceBase.cs
git commit -m "refactor: swap Newtonsoft attributes to STJ on reference base classes"
```

---

### Task 1.6: Migrate JsonExtensionObject to STJ

This is a critical file. The `ExtensionDataDeserializationConverter` needs to be rewritten for STJ.

**File:** `src/NJsonSchema/JsonExtensionObject.cs`

**Step 1: Rewrite the file**

Replace `using Newtonsoft.Json;` and `using Newtonsoft.Json.Linq;` with STJ usings.

Key changes:
- `[JsonConverter(typeof(ExtensionDataDeserializationConverter))]` → STJ `[JsonConverter]`
- `[JsonExtensionData]` → STJ `[JsonExtensionData]` (same name)
- `ExtensionDataDeserializationConverter` must be rewritten to use `Utf8JsonReader`/`Utf8JsonWriter`
- `JObject`/`JArray`/`JValue` checks → `JsonElement` / `JsonNode` pattern matching
- `obj.ToObject<JsonSchema>(serializer)` → `JsonSerializer.Deserialize<JsonSchema>(element, options)`

> **Implementation note:** The `TryDeserializeValueSchemas` method recursively walks extension data looking for objects that look like JSON schemas (have `type` or `properties` keys) and deserializes them as `JsonSchema`. This needs careful porting. The STJ converter should read the entire object into a `JsonDocument`, then walk the extension data dictionary post-deserialization to convert schema-like entries.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Run the extension data test from Phase 0**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~ExtensionData" -v n`
Expected: PASS

**Step 4: Commit**

```bash
git add src/NJsonSchema/JsonExtensionObject.cs
git commit -m "refactor: migrate ExtensionDataDeserializationConverter to STJ"
```

---

### Task 1.7: Migrate JsonSchema.Serialization.cs — the largest file

This partial class contains all the `[JsonProperty]` attributes for schema serialization and the `CreateJsonSerializerContractResolver` method.

**File:** `src/NJsonSchema/JsonSchema.Serialization.cs`

**Step 1: Replace all Newtonsoft attributes with STJ equivalents**

Pattern mapping:
| Newtonsoft | STJ |
|---|---|
| `[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]` | `[JsonPropertyName("name")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]` |
| `[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Order = N)]` | `[JsonPropertyName("name")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] [JsonPropertyOrder(N)]` |
| `[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]` | `[JsonPropertyName("name")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` |
| `[JsonConverter(typeof(...))]` | `[JsonConverter(typeof(...))]` (same name in STJ) |
| `[JsonExtensionData]` | `[JsonExtensionData]` (same name in STJ) |
| `[JsonIgnore]` | `[JsonIgnore]` (same name in STJ) |
| `[OnDeserialized]` | No direct equivalent — handle in custom converter or use `IJsonOnDeserialized` interface on net8.0+ |

**Step 2: Replace JToken/JObject/JArray usage in `*Raw` properties**

Key replacements in this file:
- `JObject` in `AdditionalPropertiesRaw` getter (Swagger2 empty object) → `new JsonObject()` or a sentinel empty object
- `JArray` in `TypeRaw` setter → `JsonArray`
- `JValue` in `TypeRaw` getter → `JsonNode` from string
- `JArray` in `EnumerationDescriptionsRaw` → `JsonArray`
- `JObject` in `DiscriminatorRaw` setter → `JsonNode` / `JsonElement` deserialization
- `ConvertPossibleStringArray(JArray?)` → `ConvertPossibleStringArray(JsonArray?)`

**Step 3: Replace `CreateJsonSerializerContractResolver` with `ConfigureJsonSerializerOptions`**

```csharp
/// <summary>Configures JsonSerializerOptions based on the SchemaType.</summary>
/// <param name="options">The options to configure.</param>
/// <param name="schemaType">The schema type.</param>
/// <returns>The converter for further customization.</returns>
public static SchemaSerializationConverter ConfigureJsonSerializerOptions(
    JsonSerializerOptions options, SchemaType schemaType)
{
    var converter = new SchemaSerializationConverter();

    if (schemaType == SchemaType.OpenApi3)
    {
        converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
        converter.RenameProperty(typeof(JsonSchemaProperty), "x-writeOnly", "writeOnly");
        converter.RenameProperty(typeof(JsonSchema), "x-nullable", "nullable");
        converter.RenameProperty(typeof(JsonSchema), "x-example", "example");
        converter.RenameProperty(typeof(JsonSchema), "x-deprecated", "deprecated");
    }
    else if (schemaType == SchemaType.Swagger2)
    {
        converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readOnly");
        converter.RenameProperty(typeof(JsonSchema), "x-example", "example");
    }
    else
    {
        converter.RenameProperty(typeof(JsonSchemaProperty), "x-readOnly", "readonly");
    }

    options.Converters.Add(converter);
    return converter;
}
```

> **Note:** Keep the old `CreateJsonSerializerContractResolver` method temporarily marked `[Obsolete]` if needed for compilation, then remove once all callers are migrated.

**Step 4: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 5: Commit**

```bash
git add src/NJsonSchema/JsonSchema.Serialization.cs
git commit -m "refactor: migrate JsonSchema.Serialization.cs from Newtonsoft to STJ"
```

---

### Task 1.8: Migrate OpenApiDiscriminator

**File:** `src/NJsonSchema/OpenApiDiscriminator.cs`

**Step 1: Replace attributes and custom converter**

- Replace `[JsonProperty("propertyName", ...)]` → `[JsonPropertyName("propertyName")]` + `[JsonIgnore(Condition = ...)]`
- Replace `[JsonProperty("mapping", ...)]` → `[JsonPropertyName("mapping")]` + `[JsonIgnore(Condition = ...)]`
- The `DiscriminatorMappingConverter` (if defined here) needs porting to STJ `JsonConverter<T>`
- Replace `[JsonIgnore]` for the `JsonInheritanceConverter` property

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/OpenApiDiscriminator.cs
git commit -m "refactor: migrate OpenApiDiscriminator to STJ attributes"
```

---

### Task 1.9: Migrate JsonSchemaSerialization infrastructure

**File:** `src/NJsonSchema/Infrastructure/JsonSchemaSerialization.cs`

**Step 1: Rewrite to use System.Text.Json**

Key changes:
- `JsonSerializerSettings` → `JsonSerializerOptions`
- `IContractResolver` parameters → `JsonSerializerOptions` parameters
- `JsonConvert.SerializeObject()` → `JsonSerializer.Serialize()`
- `JsonConvert.DeserializeObject<T>()` → `JsonSerializer.Deserialize<T>()`
- `JsonTextReader` → `JsonSerializer.Deserialize<T>(stream)`
- `Formatting.Indented` → `JsonSerializerOptions.WriteIndented = true`
- `MetadataPropertyHandling`, `ConstructorHandling`, etc. → STJ equivalents or removal
- `MaxDepth = 128` → `JsonSerializerOptions.MaxDepth = 128`

**New signature examples:**
```csharp
public static string ToJson(object obj, SchemaType schemaType, JsonSerializerOptions options)
public static Task<T> FromJsonAsync<T>(string json, SchemaType schemaType, string? documentPath,
    Func<T, JsonReferenceResolver> referenceResolverFactory, JsonSerializerOptions options,
    CancellationToken cancellationToken = default) where T : notnull
```

**Step 2: Update callers**

Update `JsonSchema.cs` methods that call `JsonSchemaSerialization`:
- `ToJson()` and `FromJsonAsync()` overloads

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 4: Run round-trip tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~RoundTrip" -v n`
Expected: PASS (tests may need updating if API signatures changed)

**Step 5: Commit**

```bash
git add src/NJsonSchema/Infrastructure/JsonSchemaSerialization.cs src/NJsonSchema/JsonSchema.cs
git commit -m "refactor: migrate JsonSchemaSerialization to System.Text.Json"
```

---

### Task 1.10: Migrate remaining infrastructure files

**Files:**
- `src/NJsonSchema/Infrastructure/TypeExtensions.cs` — remove `JsonPropertyAttribute` usage (line 33)
- `src/NJsonSchema/JsonPathUtilities.cs` — remove `DefaultContractResolver` usage, replace with STJ options
- `src/NJsonSchema/JsonSchemaReferenceUtilities.cs` — remove `DefaultContractResolver` usage
- `src/NJsonSchema/JsonReferenceResolver.cs` — remove `JsonIgnoreAttribute` check, replace with STJ equivalent
- `src/NJsonSchema/Visitors/JsonReferenceVisitorBase.cs` — update Newtonsoft references
- `src/NJsonSchema/Visitors/AsyncJsonReferenceVisitorBase.cs` — update Newtonsoft references

**Step 1: Update each file**

For each file, replace Newtonsoft imports and usages with STJ equivalents. The reference utilities that use `IContractResolver` to discover properties need to switch to reflection or STJ's `JsonSerializerOptions` for property discovery.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/Infrastructure/TypeExtensions.cs src/NJsonSchema/JsonPathUtilities.cs src/NJsonSchema/JsonSchemaReferenceUtilities.cs src/NJsonSchema/JsonReferenceResolver.cs src/NJsonSchema/Visitors/
git commit -m "refactor: migrate remaining infrastructure files from Newtonsoft to STJ"
```

---

### Task 1.11: Migrate schema generation files

**Files:**
- `src/NJsonSchema/Generation/JsonSchemaGenerator.cs` — remove Newtonsoft imports, replace JToken type checks
- `src/NJsonSchema/Generation/ReflectionServiceBase.cs` — remove JToken/JObject/JArray type name checks (lines 197-203)

**Step 1: Update JsonSchemaGenerator.cs**

Remove `using Newtonsoft.Json;` and `using Newtonsoft.Json.Linq;`. Replace any remaining JToken type checks with `System.Text.Json.Nodes.JsonNode` equivalents.

**Step 2: Update ReflectionServiceBase.cs**

Replace string comparisons for Newtonsoft types:
```csharp
// Before:
"Newtonsoft.Json.Linq.JArray"
"Newtonsoft.Json.Linq.JToken"
"Newtonsoft.Json.Linq.JObject"

// After:
"System.Text.Json.Nodes.JsonArray"
"System.Text.Json.Nodes.JsonNode"
"System.Text.Json.Nodes.JsonObject"
```

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 4: Commit**

```bash
git add src/NJsonSchema/Generation/JsonSchemaGenerator.cs src/NJsonSchema/Generation/ReflectionServiceBase.cs
git commit -m "refactor: migrate schema generation from Newtonsoft to STJ"
```

---

### Task 1.12: Delete old contract resolvers

**Files:**
- Delete: `src/NJsonSchema/Infrastructure/PropertyRenameAndIgnoreSerializerContractResolver.cs`
- Delete: `src/NJsonSchema/Infrastructure/IgnoreEmptyCollectionsContractResolver.cs`

**Step 1: Verify no remaining references**

Search for `PropertyRenameAndIgnoreSerializerContractResolver` and `IgnoreEmptyCollectionsContractResolver` in the codebase. All references should have been replaced by `SchemaSerializationConverter` in previous tasks.

**Step 2: Delete the files**

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 4: Commit**

```bash
git rm src/NJsonSchema/Infrastructure/PropertyRenameAndIgnoreSerializerContractResolver.cs src/NJsonSchema/Infrastructure/IgnoreEmptyCollectionsContractResolver.cs
git commit -m "refactor: remove old Newtonsoft contract resolvers"
```

---

### Task 1.13: Run full test suite checkpoint

**Step 1: Run all tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -v n`

**Step 2: Fix any failing tests**

Tests that construct `JToken` for validation or use `JsonConvert` will fail — that's expected, they'll be fixed in Phase 3 and Phase 7. Focus on serialization/deserialization tests passing.

**Step 3: Run snapshot tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~Snapshot" -v n`

If snapshots changed, review the diffs carefully. If the JSON output structure is identical but formatting differs (e.g., STJ vs Newtonsoft whitespace), accept the new snapshots.

**Step 4: Commit any test fixes**

```bash
git add -A
git commit -m "test: fix tests after Phase 1 model/serialization migration"
```

---

## Phase 2: Validation System

### Task 2.1: Replace JToken with JsonNode in IFormatValidator

**File:** `src/NJsonSchema/Validation/FormatValidators/IFormatValidator.cs`

**Step 1: Update the interface**

```csharp
// Before:
using Newtonsoft.Json.Linq;
bool IsValid(string value, JTokenType tokenType);

// After:
using System.Text.Json.Nodes;
bool IsValid(string value, JsonValueKind tokenType);
```

> **Note:** `JTokenType` maps to `JsonValueKind` from System.Text.Json:
> - `JTokenType.String` → `JsonValueKind.String`
> - `JTokenType.Integer` → `JsonValueKind.Number`
> - `JTokenType.Float` → `JsonValueKind.Number`
> - `JTokenType.Boolean` → `JsonValueKind.True` or `JsonValueKind.False`
> - `JTokenType.Null` → `JsonValueKind.Null`
> - `JTokenType.Object` → `JsonValueKind.Object`
> - `JTokenType.Array` → `JsonValueKind.Array`

**Step 2: Build (will fail — that's expected, format validators need updating next)**

---

### Task 2.2: Update all format validators

**Files** (14 files in `src/NJsonSchema/Validation/FormatValidators/`):
- `Base64FormatValidator.cs`
- `ByteFormatValidator.cs`
- `DateFormatValidator.cs`
- `DateTimeFormatValidator.cs`
- `EmailFormatValidator.cs`
- `GuidFormatValidator.cs`
- `HostnameFormatValidator.cs`
- `IpV4FormatValidator.cs`
- `IpV6FormatValidator.cs`
- `TimeFormatValidator.cs`
- `TimeSpanFormatValidator.cs`
- `UriFormatValidator.cs`
- `UuidFormatValidator.cs`

**Step 1: In each file:**

Replace `using Newtonsoft.Json.Linq;` with `using System.Text.Json;`

Replace `JTokenType` parameter with `JsonValueKind`:
```csharp
// Before:
public bool IsValid(string value, JTokenType tokenType)

// After:
public bool IsValid(string value, JsonValueKind tokenType)
```

Replace `JTokenType.String` comparisons with `JsonValueKind.String`, etc.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/Validation/FormatValidators/
git commit -m "refactor: migrate format validators from JTokenType to JsonValueKind"
```

---

### Task 2.3: Migrate ValidationError

**File:** `src/NJsonSchema/Validation/ValidationError.cs`

**Step 1: Update the class**

- Replace `JToken? token` parameter → `JsonNode? token`
- Replace `JToken? Token` property → `JsonNode? Token`
- Remove `IJsonLineInfo` line info extraction (JsonNode doesn't carry line info)
  - `HasLineInfo` will be `false` by default
  - Consider: add an overload that accepts line info separately, or remove the feature

```csharp
using System.Text.Json.Nodes;

public class ValidationError
{
    public ValidationError(ValidationErrorKind errorKind, string? propertyName, string? propertyPath, JsonNode? token, JsonSchema schema)
    {
        Kind = errorKind;
        Property = propertyName;
        Path = propertyPath != null ? "#/" + propertyPath : "#";
        Token = token;
        HasLineInfo = false;
        LineNumber = 0;
        LinePosition = 0;
        Schema = schema;
    }

    // ... properties unchanged except Token type
    public JsonNode? Token { get; private set; }
}
```

**Step 2: Update ChildSchemaValidationError and MultiTypeValidationError**

Update their constructors to use `JsonNode?` instead of `JToken?`.

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 4: Commit**

```bash
git add src/NJsonSchema/Validation/ValidationError.cs src/NJsonSchema/Validation/ChildSchemaValidationError.cs src/NJsonSchema/Validation/MultiTypeValidationError.cs
git commit -m "refactor: migrate ValidationError from JToken to JsonNode"
```

---

### Task 2.4: Migrate JsonSchemaValidator

**File:** `src/NJsonSchema/Validation/JsonSchemaValidator.cs`

This is the largest validation file (~41 Newtonsoft references). The core change is replacing `JToken` with `JsonNode`.

**Step 1: Update the Validate(string) method**

```csharp
// Before:
public ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
{
    using var reader = new StringReader(jsonData);
    using var jsonReader = new JsonTextReader(reader) { DateParseHandling = DateParseHandling.None };
    var jsonObject = JToken.ReadFrom(jsonReader);
    return Validate(jsonObject, schema, schemaType);
}

// After:
public ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
{
    var jsonNode = JsonNode.Parse(jsonData);
    return Validate(jsonNode, schema, schemaType);
}
```

**Step 2: Update the Validate(JToken) method → Validate(JsonNode?)**

```csharp
// Before:
public ICollection<ValidationError> Validate(JToken token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)

// After:
public ICollection<ValidationError> Validate(JsonNode? token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema)
```

**Step 3: Replace all JToken type checks throughout the file**

Key pattern mapping for the validator:
```csharp
// JToken type checks → JsonNode pattern matching
token.Type == JTokenType.Object    →  token is JsonObject
token.Type == JTokenType.Array     →  token is JsonArray
token.Type == JTokenType.String    →  token is JsonValue v && v.GetValueKind() == JsonValueKind.String
token.Type == JTokenType.Integer   →  token is JsonValue v && v.GetValueKind() == JsonValueKind.Number
token.Type == JTokenType.Float     →  token is JsonValue v && v.GetValueKind() == JsonValueKind.Number
token.Type == JTokenType.Boolean   →  token is JsonValue v && (v.GetValueKind() == JsonValueKind.True || v.GetValueKind() == JsonValueKind.False)
token.Type == JTokenType.Null      →  token == null

// JToken value access → JsonNode value access
token.Value<string>()              →  token.GetValue<string>()
token.Value<int>()                 →  token.GetValue<int>()
token.Value<double>()              →  token.GetValue<double>()
((JObject)token).Properties()      →  ((JsonObject)token).Select(p => p)
((JObject)token)[key]              →  ((JsonObject)token)[key]
((JArray)token).Count              →  ((JsonArray)token).Count
((JArray)token)[i]                 →  ((JsonArray)token)[i]
token.ToString()                   →  token.ToJsonString()
```

> **Important:** `JsonNode` treats `null` JSON values differently from JToken. In JToken, `null` is a `JValue` with `Type == JTokenType.Null`. In JsonNode, parsing `"null"` returns `null` (the C# null). The validator needs null checks where JToken had `JTokenType.Null` checks.

**Step 4: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 5: Run validation regression tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~ValidationRegressionTests" -v n`
Expected: PASS (these tests use `Validate(string)`, which should work)

**Step 6: Commit**

```bash
git add src/NJsonSchema/Validation/JsonSchemaValidator.cs
git commit -m "refactor: migrate JsonSchemaValidator from JToken to JsonNode"
```

---

### Task 2.5: Run validation test suite

**Step 1: Run all validation tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~Validation" -v n`

**Step 2: Fix failures**

Tests that directly construct `JToken`/`JObject`/`JValue` and pass to `Validate()` will need updating. For now, update these tests to use the `Validate(string)` overload or `JsonNode.Parse()`.

**Step 3: Commit fixes**

```bash
git add -A
git commit -m "test: fix validation tests after JToken to JsonNode migration"
```

---

## Phase 3: Sample Data Generation

### Task 3.1: Migrate SampleJsonDataGenerator

**File:** `src/NJsonSchema/Generation/SampleJsonDataGenerator.cs`

**Step 1: Replace all JToken construction with JsonNode**

```csharp
// Before:
using Newtonsoft.Json.Linq;

new JObject()           →  new JsonObject()
new JArray()            →  new JsonArray()
new JValue("text")      →  JsonValue.Create("text")
new JValue(42)          →  JsonValue.Create(42)
new JValue(true)        →  JsonValue.Create(true)
JToken.FromObject(obj)  →  JsonSerializer.SerializeToNode(obj)
jobj["key"] = value     →  jsonObj["key"] = value  (same syntax)
jarray.Add(item)        →  jsonArray.Add(item)
```

Update return type from `JToken` to `JsonNode`.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/Generation/SampleJsonDataGenerator.cs
git commit -m "refactor: migrate SampleJsonDataGenerator from JToken to JsonNode"
```

---

### Task 3.2: Migrate SampleJsonSchemaGenerator

**File:** `src/NJsonSchema/SampleJsonSchemaGenerator.cs`

**Step 1: Replace JToken/JObject/JArray usage with JsonNode equivalents**

Same pattern as Task 3.1.

**Step 2: Build to verify**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`

**Step 3: Commit**

```bash
git add src/NJsonSchema/SampleJsonSchemaGenerator.cs
git commit -m "refactor: migrate SampleJsonSchemaGenerator from JToken to JsonNode"
```

---

### Task 3.3: Fix sample data generation tests

**Step 1: Update tests to use JsonNode instead of JToken**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~SampleJsonDataGenerator" -v n`

Fix all tests that reference `JToken`, `JObject`, `JArray`, `JValue` — replace with `JsonNode`, `JsonObject`, `JsonArray`, `JsonValue`.

**Step 2: Commit**

```bash
git add -A
git commit -m "test: fix sample data generation tests after JsonNode migration"
```

---

## Phase 4: Peripheral Packages

### Task 4.1: Migrate NJsonSchema.CodeGeneration

**File:** `src/NJsonSchema.CodeGeneration/CodeGeneratorSettingsBase.cs`

**Step 1: Replace `using Newtonsoft.Json;` with `using System.Text.Json.Serialization;`**

The `[JsonIgnore]` attributes stay the same name.

**Step 2: Update .csproj if it references Newtonsoft.Json**

Check `src/NJsonSchema.CodeGeneration/NJsonSchema.CodeGeneration.csproj` — remove any `Newtonsoft.Json` `PackageReference`.

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema.CodeGeneration/NJsonSchema.CodeGeneration.csproj`

**Step 4: Commit**

```bash
git add src/NJsonSchema.CodeGeneration/
git commit -m "refactor: remove Newtonsoft.Json from NJsonSchema.CodeGeneration"
```

---

### Task 4.2: Migrate NJsonSchema.Yaml

**File:** `src/NJsonSchema.Yaml/JsonSchemaYaml.cs`

**Step 1: Replace the `ToYaml()` method**

```csharp
// Before:
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public static string ToYaml(this JsonSchema document)
{
    var json = document.ToJson()!;
    var expConverter = new ExpandoObjectConverter();
    dynamic? deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);
    var serializer = new Serializer();
    return serializer.Serialize(deserializedObject);
}

// After:
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

public static string ToYaml(this JsonSchema document)
{
    var json = document.ToJson()!;
    var jsonNode = JsonNode.Parse(json);
    var expandoObject = ConvertJsonNodeToExpando(jsonNode);
    var serializer = new Serializer();
    return serializer.Serialize(expandoObject);
}

private static object? ConvertJsonNodeToExpando(JsonNode? node)
{
    if (node is JsonObject obj)
    {
        var expando = new ExpandoObject() as IDictionary<string, object?>;
        foreach (var prop in obj)
        {
            expando[prop.Key] = ConvertJsonNodeToExpando(prop.Value);
        }
        return expando;
    }
    if (node is JsonArray array)
    {
        return array.Select(ConvertJsonNodeToExpando).ToList();
    }
    if (node is JsonValue value)
    {
        var element = value.GetValue<JsonElement>();
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
    return null;
}
```

**Step 2: Remove Newtonsoft.Json package reference if present in .csproj**

**Step 3: Build to verify**

Run: `dotnet build src/NJsonSchema.Yaml/NJsonSchema.Yaml.csproj`

**Step 4: Run YAML tests**

Run: `dotnet test src/NJsonSchema.Yaml.Tests/NJsonSchema.Yaml.Tests.csproj -v n`

**Step 5: Commit**

```bash
git add src/NJsonSchema.Yaml/
git commit -m "refactor: remove Newtonsoft.Json from NJsonSchema.Yaml"
```

---

## Phase 5: Remove Newtonsoft Dependency from Core

### Task 5.1: Update NJsonSchema.csproj

**File:** `src/NJsonSchema/NJsonSchema.csproj`

**Step 1: Remove Newtonsoft.Json PackageReference**

```xml
<!-- Remove this line: -->
<PackageReference Include="Newtonsoft.Json" />
```

**Step 2: Ensure System.Text.Json is referenced for all TFMs**

```xml
<!-- Keep existing conditional for older TFMs: -->
<ItemGroup Condition="!$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">
    <PackageReference Include="System.Text.Json" />
</ItemGroup>
```

**Step 3: Build all TFMs**

Run: `dotnet build src/NJsonSchema/NJsonSchema.csproj`
Expected: BUILD SUCCEEDED on all three TFMs (netstandard2.0, net462, net8.0)

If any Newtonsoft references remain, find and fix them.

**Step 4: Commit**

```bash
git add src/NJsonSchema/NJsonSchema.csproj
git commit -m "feat: remove Newtonsoft.Json dependency from NJsonSchema core"
```

---

### Task 5.2: Verify all downstream projects build

**Step 1: Build everything**

Run: `dotnet build src/NJsonSchema.sln`
Expected: BUILD SUCCEEDED

**Step 2: Fix any remaining compilation errors**

**Step 3: Commit fixes**

```bash
git add -A
git commit -m "fix: resolve remaining build errors after Newtonsoft removal"
```

---

## Phase 6: Test Updates

### Task 6.1: Update test projects — replace JToken usage in tests

**Files:** All test files in `src/NJsonSchema.Tests/` that use `Newtonsoft.Json.Linq`

**Step 1: Find all test files using JToken**

Search for `using Newtonsoft.Json.Linq;` in test projects.

**Step 2: Replace JToken patterns**

```csharp
// Before:
using Newtonsoft.Json.Linq;
var token = JToken.Parse(json);
schema.Validate(token, schema);
Assert.Equal(JTokenType.String, ((JObject)token)["name"]!.Type);

// After:
using System.Text.Json.Nodes;
var node = JsonNode.Parse(json);
schema.Validate(node, schema);
Assert.IsType<JsonValue>(((JsonObject)node!)["name"]);
```

**Step 3: Run full test suite**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -v n`

**Step 4: Fix failures iteratively**

**Step 5: Commit**

```bash
git add -A
git commit -m "test: migrate all tests from JToken to JsonNode"
```

---

### Task 6.2: Update CodeGeneration test projects

**Step 1: Run tests**

Run: `dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj -v n`
Run: `dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -v n`

**Step 2: Fix any failures**

**Step 3: Commit**

```bash
git add -A
git commit -m "test: fix CodeGeneration tests after STJ migration"
```

---

### Task 6.3: Update snapshot files

**Step 1: Run snapshot tests**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj --filter "FullyQualifiedName~Snapshot" -v n`

**Step 2: Review and accept new snapshots**

If JSON output differs due to STJ formatting differences (e.g., property ordering, whitespace), review carefully and accept.

**Step 3: Commit**

```bash
git add -A
git commit -m "test: update snapshots after STJ migration"
```

---

### Task 6.4: Final full test suite run

**Step 1: Run everything**

Run: `dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj -v n`
Run: `dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj -v n`
Run: `dotnet test src/NJsonSchema.CodeGeneration.TypeScript.Tests/NJsonSchema.CodeGeneration.TypeScript.Tests.csproj -v n`
Run: `dotnet test src/NJsonSchema.Yaml.Tests/NJsonSchema.Yaml.Tests.csproj -v n`

Expected: ALL PASS

**Step 2: Compare test count with Phase 0 baseline**

Ensure no tests were accidentally deleted.

**Step 3: Commit**

```bash
git commit --allow-empty -m "milestone: STJ migration complete — all tests passing"
```

---

## Phase 7: Cleanup

### Task 7.1: Remove dead code

- Delete any `#if` blocks that referenced Newtonsoft-specific paths
- Remove any `[Obsolete]` shims added during migration
- Clean up unused `using` statements

### Task 7.2: Update CLAUDE.md

Update the project documentation to reflect that core now uses System.Text.Json.

### Task 7.3: Final review

Use `superpowers:requesting-code-review` to review the entire migration diff.

---

## Summary

| Phase | Tasks | Focus |
|-------|-------|-------|
| 0 | 0.1–0.6 | Test hardening — lock down behavior before migration |
| 1 | 1.1–1.13 | Schema model + serialization infrastructure (largest phase) |
| 2 | 2.1–2.5 | Validation system (JToken → JsonNode) |
| 3 | 3.1–3.3 | Sample data generation |
| 4 | 4.1–4.2 | Peripheral packages (CodeGeneration, Yaml) |
| 5 | 5.1–5.2 | Remove Newtonsoft PackageReference |
| 6 | 6.1–6.4 | Test updates and snapshot fixes |
| 7 | 7.1–7.3 | Cleanup and review |

**Total: ~30 tasks across 8 phases**
