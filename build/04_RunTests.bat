dotnet test "%~dp0/../src\NJsonSchema.Tests\NJsonSchema.Tests.csproj" -c Release
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.Tests\NJsonSchema.CodeGeneration.Tests.csproj" -c Release
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.CSharp.Tests\NJsonSchema.CodeGeneration.CSharp.Tests.csproj" -c Release
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.TypeScript.Tests\NJsonSchema.CodeGeneration.TypeScript.Tests.csproj" -c Release
