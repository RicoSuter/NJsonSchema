#!/bin/bash

(
	cd src/NJsonSchema
	dotnet pack -c Release
)

(
	cd src/NJsonSchema.CodeGeneration.TypeScript
	dotnet pack -c Release
)

(
	cd src/NJsonSchema.CodeGeneration.CSharp
	dotnet pack -c Release
)