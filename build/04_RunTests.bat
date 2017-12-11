dotnet test "src\NJsonSchema.Tests\NJsonSchema.Tests.csproj" -c %CONFIGURATION%
dotnet test "src\NJsonSchema.CodeGeneration\NJsonSchema.CodeGeneration.csproj" -c %CONFIGURATION%
dotnet test "src\NJsonSchema.CodeGeneration.CSharp.Tests\NJsonSchema.CodeGeneration.CSharp.Tests.csproj" -c %CONFIGURATION%
