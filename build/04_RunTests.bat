dotnet test "%~dp0/../src\NJsonSchema.Tests\NJsonSchema.Tests.csproj" -c Release || goto :error
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.Tests\NJsonSchema.CodeGeneration.Tests.csproj" -c Release || goto :error
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.CSharp.Tests\NJsonSchema.CodeGeneration.CSharp.Tests.csproj" -c Release || goto :error
dotnet test "%~dp0/../src\NJsonSchema.CodeGeneration.TypeScript.Tests\NJsonSchema.CodeGeneration.TypeScript.Tests.csproj" -c Release || goto :error
dotnet test "%~dp0/../src\NJsonSchema.Yaml.Tests\NJsonSchema.Yaml.Tests.csproj" -c Release || goto :error

goto :EOF
:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%
