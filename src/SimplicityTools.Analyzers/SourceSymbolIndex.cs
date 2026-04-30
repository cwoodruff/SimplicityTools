using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Analyzers;

internal sealed class SourceSymbolIndex
{
    private readonly Dictionary<IMethodSymbol, MethodDeclarationSyntax> _methodDeclarations;
    private readonly Dictionary<INamedTypeSymbol, string> _typeFiles;

    private SourceSymbolIndex(
        Dictionary<IMethodSymbol, MethodDeclarationSyntax> methodDeclarations,
        Dictionary<INamedTypeSymbol, string> typeFiles)
    {
        _methodDeclarations = methodDeclarations;
        _typeFiles = typeFiles;
    }

    public ImmutableArray<INamedTypeSymbol> NamedTypes => [.. _typeFiles.Keys];

    public ImmutableArray<IMethodSymbol> Methods => [.. _methodDeclarations.Keys];

    public bool TryGetMethodDeclaration(IMethodSymbol method, out MethodDeclarationSyntax declaration)
        => _methodDeclarations.TryGetValue(method, out declaration!);

    public bool TryGetFilePath(INamedTypeSymbol namedType, out string filePath)
        => _typeFiles.TryGetValue(namedType, out filePath!);

    public static SourceSymbolIndex Create(Compilation compilation, CancellationToken cancellationToken)
    {
        var methodDeclarations = new Dictionary<IMethodSymbol, MethodDeclarationSyntax>(SymbolEqualityComparer.Default);
        var typeFiles = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath))
            {
                continue;
            }

            var root = syntaxTree.GetRoot(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            foreach (var typeDeclaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is INamedTypeSymbol namedType)
                {
                    typeFiles[namedType.OriginalDefinition] = syntaxTree.FilePath;
                }
            }

            foreach (var methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is IMethodSymbol method &&
                    method.MethodKind == MethodKind.Ordinary)
                {
                    methodDeclarations[method.OriginalDefinition] = methodDeclaration;
                }
            }
        }

        return new SourceSymbolIndex(methodDeclarations, typeFiles);
    }
}
