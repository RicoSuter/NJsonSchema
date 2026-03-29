# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NJsonSchema is a .NET library for reading, generating, and validating JSON Schema draft v4+ schemas. It also provides C# and TypeScript code generation from JSON schemas. The library is heavily used by [NSwag](http://nswag.org).

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

## Version Management

- Version defined in `Directory.Build.props`
- Git tags (v*.*.*) trigger production NuGet releases
- Master branch builds go to MyGet preview feed
