"nuget/nuget" restore NJsonSchema.sln
msbuild NJsonSchema.sln /p:Configuration=Release /t:rebuild
"nuget/nuget" pack NJsonSchema/NJsonSchema.csproj -OutputDirectory "NuGet" -Prop Configuration=Release
