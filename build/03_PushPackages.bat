set /p apiKey=NuGet API Key: 
set /p version=Package Version: 

nuget.exe push Packages/NJsonSchema.%version%.nupkg %apiKey%
nuget.exe push Packages/NJsonSchema.CodeGeneration.%version%.nupkg %apiKey%