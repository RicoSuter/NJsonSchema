nuget restore ../src/NJsonSchema.sln
msbuild ../src/NJsonSchema.sln /p:Configuration=Release /t:rebuild
