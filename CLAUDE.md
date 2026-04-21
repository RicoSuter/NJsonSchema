# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NJsonSchema is a .NET library for reading, generating, and validating JSON Schema draft v4+ schemas. It also provides C# and TypeScript code generation from JSON schemas. The library is heavily used by [NSwag](http://nswag.org).

## Cross-check with NSwag

**NJsonSchema is consumed directly by NSwag** (OpenAPI document generation, client/server code generation, Swagger 2.0 / OpenAPI 3.x support). A sibling clone of the NSwag repo is expected at `../NSwag` (relative to this repo). Treat NSwag as a first-class consumer — if a change works for standalone NJsonSchema but breaks NSwag, the change isn't ready.

### Supported spec targets

All of the following are first-class targets. Every change should preserve or improve behavior across all of them:

- **JSON Schema** drafts 4, 6, 7, 2019-09, and 2020-12.
- **Swagger 2.0** (OpenAPI 2.0) — JSON Schema Draft 4 subset.
- **OpenAPI 3.0.x** — Draft 5 subset with 3.0-specific nullability (`nullable: true`).
- **OpenAPI 3.1** — aligned with JSON Schema 2020-12. This includes native `const`, arbitrary keyword siblings alongside `$ref`, and type-array nullability (`type: ["string", "null"]`). New features and bug fixes should work correctly under 3.1 semantics, not just the older drafts.

`SchemaType` (`JsonSchema` / `Swagger2` / `OpenApi3`) currently conflates 3.0 and 3.1 into a single enum value; where 3.1-specific behavior matters, infer from the schema contents (presence of 2020-12 keywords, type arrays, etc.) until a dedicated enum value exists.

**Every non-trivial change to NJsonSchema — new features, behavior changes, refactors, renames, public-API removals — needs to be evaluated through NSwag's lens as well, both when authoring and when reviewing:**

1. **Check virtual / overridable members.** NSwag subclasses core types. For example, `NSwag.Core.OpenApiParameter : JsonSchema` overrides `ActualSchema`; `OpenApiDocument` and `OpenApiOperation` extend base types. Any semantic shift to a virtual member's contract can break NSwag transparently.
2. **Grep the sibling `../NSwag` clone** for usages of the symbol or behavior you're changing. NSwag consumers live primarily under `src/NSwag.Core/`, `src/NSwag.Generation/`, and `src/NSwag.CodeGeneration.*/`. If the symbol is referenced there, weigh the change through NSwag's usage too.
3. **Public-API changes (rename, remove, change signature) are NSwag contract changes.** Even if a member looks unused internally, NSwag or its plugins may depend on it. Confirm against `../NSwag` before renaming or removing.
4. **Verify behavior across all three `SchemaType` values** (`JsonSchema`, `Swagger2`, `OpenApi3`). A keyword, validator, serializer, or codegen path that works for standalone JSON Schema may be silently wrong under Swagger 2.0 (Draft 4) or OpenAPI 3.0 (Draft 5 subset), which have narrower keyword support than 3.1 / 2020-12. Pick spec-appropriate idioms (e.g. `enum: [x]` in 2.0/3.0, `const: x` in 3.1).
5. **When in doubt, run NSwag's tests against your build.** Point NSwag at the locally built NJsonSchema packages and run `build.sh Test` there.

Reviewers: explicitly ask "has this been cross-checked against NSwag?" for any PR touching `JsonSchema`, validation, serialization, code generation, or public API surface.

Reference resolution (`ActualSchema` vs `ActualTypeSchema`, `$ref` semantics, sibling keyword handling) is one common area where NJsonSchema/NSwag interactions matter — see `docs/references.md` for the details, the resolution algorithm, and known cross-keyword limitations.

## Build Commands

The project uses NUKE build automation. Requires .NET 9.0 SDK.

```bash
# Full build with tests
./build.cmd Compile Test Pack        # Windows
./build.sh Compile Test Pack         # Linux/Mac

# Compile only
./build.sh Compile

# Run all tests
./build.sh Test

# Run a single test project
dotnet test src/NJsonSchema.Tests/NJsonSchema.Tests.csproj
dotnet test src/NJsonSchema.CodeGeneration.CSharp.Tests/NJsonSchema.CodeGeneration.CSharp.Tests.csproj

# Pack NuGet packages (output to artifacts/)
./build.sh Pack
```

## Architecture

### Core Packages

- **NJsonSchema** - Core library: schema parsing, validation, generation from .NET types
- **NJsonSchema.Annotations** - Attributes for controlling schema generation
- **NJsonSchema.CodeGeneration** - Base classes for code generation
- **NJsonSchema.CodeGeneration.CSharp** - C# code generator with Liquid templates
- **NJsonSchema.CodeGeneration.TypeScript** - TypeScript code generator
- **NJsonSchema.NewtonsoftJson** - Newtonsoft.Json integration
- **NJsonSchema.Yaml** - YAML schema support

### Key Classes

- `JsonSchema` - Main schema class for parsing and validation
- `JsonSchemaGenerator` - Generates schemas from .NET types via reflection
- `CSharpGenerator` / `TypeScriptGenerator` - Code generators using Liquid templates

### Code Generation Templates

Templates are in `src/NJsonSchema.CodeGeneration.CSharp/Templates/` and `src/NJsonSchema.CodeGeneration.TypeScript/Templates/` using Liquid template engine (Fluid.Core).

## Testing

- Uses **XUnit v3** with **Verify** for snapshot testing
- Snapshot files are in `Snapshots/` directories with `.verified.txt` extension
- Snapshots use UTF-8 with BOM and LF line endings (see `.editorconfig`)
- Always use AAA (Arrange/Act/Assert) pattern with `// Arrange`, `// Act`, `// Assert` comments, matching existing test style

## Code Style

- C# latest language version with nullable enabled
- Warnings treated as errors
- 4-space indentation, CRLF line endings (except `.verified.txt` files)
- Prefer System.Text.Json over Newtonsoft.Json for new code
- Consider AOT compatibility (use generic converters like `JsonStringEnumConverter<T>`)
- Do not use abbreviations in variable/field names (e.g. use `attribute` not `attr`, `property` not `prop`, `parameter` not `param`)

## Git Rules

- Never include "Claude", "Co-Authored-By", or AI attribution in commit messages, PR descriptions, or GitHub comments.
