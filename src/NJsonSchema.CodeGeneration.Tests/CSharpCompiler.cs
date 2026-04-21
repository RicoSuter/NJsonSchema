using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NJsonSchema.CodeGeneration.CSharp.Tests;

public class CSharpCompiler
{
    public static Assembly AssertCompile(string source, CSharpParseOptions options = null, bool returnAssembly = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, options);

        var metadataReferences = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Append(MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RangeAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(System.Collections.ObjectModel.ObservableCollection<>).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonConverter).Assembly.Location))
            .ToList();

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "assemblyName",
            syntaxTrees: [syntaxTree],
            references:
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var dllStream = new MemoryStream();

        var emitResult = compilation.Emit(dllStream);
        if (!emitResult.Success)
        {
            // emitResult.Diagnostics
            Assert.Empty(emitResult.Diagnostics);
        }

        if (returnAssembly)
        {
            return Assembly.Load(dllStream.GetBuffer());
        }

        return null;
    }
}