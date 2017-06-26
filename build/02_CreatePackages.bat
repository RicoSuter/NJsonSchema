nuget pack ../src/NJsonSchema/NJsonSchema.csproj -IncludeReferencedProjects -OutputDirectory "Packages" -Prop Configuration=Release
nuget pack ../src/NJsonSchema.CodeGeneration/NJsonSchema.CodeGeneration.csproj -IncludeReferencedProjects -OutputDirectory "Packages" -Prop Configuration=Release
