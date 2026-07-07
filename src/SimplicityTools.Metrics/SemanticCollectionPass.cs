using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Metrics;

internal sealed class SemanticCollectionPass
{
    public async Task<SemanticMetrics> CollectAsync(Solution solution, IReadOnlySet<string> projectFilePaths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(projectFilePaths);

        // Keys are compilation-independent identities (assembly + fully-qualified name) so the
        // same interface or implementation seen from sibling-TFM compilations counts once and
        // cross-project matches survive metadata-reference fallback.
        var abstractionLayers = new HashSet<string>(StringComparer.Ordinal);
        var implementationMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var methodComplexities = new List<int>();
        var declaredPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in SelectAnalyzableProjects(solution, projectFilePaths))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Could not build a Roslyn compilation for '{project.FilePath ?? project.Name}'.");

            var packageAssets = PackageAssetsReader.TryRead(project.FilePath);
            if (packageAssets is not null)
            {
                declaredPackages.UnionWith(packageAssets.DeclaredPackageIds);

                // Analyzer, build-asset, placeholder, and meta-packages contribute no compile
                // assembly; "no detected symbol usage" is meaningless for them.
                usedPackages.UnionWith(packageAssets.BuildOnlyPackageIds);
                usedPackages.UnionWith(ResolveUsedPackages(compilation, packageAssets.AssemblyNamesByPackageId, cancellationToken));
            }

            foreach (var type in EnumerateNamedTypes(compilation.Assembly.GlobalNamespace))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!HasCountableSourceDeclaration(type))
                {
                    continue;
                }

                var typeIdentity = GetSymbolIdentity(type);
                if (type.TypeKind == TypeKind.Interface)
                {
                    abstractionLayers.Add(typeIdentity);
                    implementationMap.TryAdd(typeIdentity, []);
                    continue;
                }

                if (!IsConcreteImplementation(type))
                {
                    continue;
                }

                foreach (var implementedInterface in type.AllInterfaces)
                {
                    var interfaceIdentity = GetSymbolIdentity(implementedInterface);
                    if (!implementationMap.TryGetValue(interfaceIdentity, out var implementations))
                    {
                        implementations = [];
                        implementationMap.Add(interfaceIdentity, implementations);
                    }

                    implementations.Add(typeIdentity);
                }
            }

            foreach (var syntaxTree in compilation.SyntaxTrees.Where(ShouldAnalyzeSyntaxTree))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                methodComplexities.AddRange(CyclomaticComplexityAnalyzer.Collect(root));
            }
        }

        return new SemanticMetrics(
            abstractionLayers.Count,
            abstractionLayers.Count(interfaceIdentity => implementationMap.TryGetValue(interfaceIdentity, out var implementations) && implementations.Count == 1),
            methodComplexities.Count > 0 ? methodComplexities.Average() : 0d,
            declaredPackages.Count,
            declaredPackages.Count(packageId => !usedPackages.Contains(packageId)));
    }

    /// <summary>
    /// Selects the projects to analyze: declared by the solution (the workspace also loads
    /// project-referenced projects from outside it), not test projects, and — because a
    /// multi-targeted project appears in the workspace once per target framework — one workspace
    /// instance per project file so nothing double-counts.
    /// </summary>
    internal static IEnumerable<Project> SelectAnalyzableProjects(Solution solution, IReadOnlySet<string> projectFilePaths)
    {
        return solution.Projects
            .Where(project => project.FilePath is not null && projectFilePaths.Contains(project.FilePath))
            .Where(project => !SourceFileConventions.IsTestProject(project.FilePath, project.Name))
            .GroupBy(project => project.FilePath!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }

    private static string GetSymbolIdentity(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;
        var assemblyName = definition.ContainingAssembly?.Identity.Name ?? string.Empty;
        return $"{assemblyName}|{definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
    }

    private static bool ShouldAnalyzeSyntaxTree(SyntaxTree syntaxTree)
    {
        return SourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath);
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            foreach (var nestedType in EnumerateNamedTypes(type))
            {
                yield return nestedType;
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in EnumerateNamedTypes(nestedNamespace))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamedTypeSymbol typeSymbol)
    {
        yield return typeSymbol;

        foreach (var nestedType in typeSymbol.GetTypeMembers())
        {
            foreach (var descendant in EnumerateNamedTypes(nestedType))
            {
                yield return descendant;
            }
        }
    }

    private static bool HasCountableSourceDeclaration(ISymbol symbol)
    {
        return symbol.DeclaringSyntaxReferences.Any(reference => SourceFileConventions.IsCountableSourceFile(reference.SyntaxTree.FilePath));
    }

    private static bool IsConcreteImplementation(INamedTypeSymbol type)
    {
        return (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) &&
               !type.IsAbstract &&
               type.AllInterfaces.Length > 0;
    }

    private static IReadOnlySet<string> ResolveUsedPackages(
        Compilation compilation,
        IReadOnlyDictionary<string, IReadOnlySet<string>> assemblyNamesByPackageId,
        CancellationToken cancellationToken)
    {
        var usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (assemblyNamesByPackageId.Count == 0)
        {
            return usedPackages;
        }

        foreach (var syntaxTree in compilation.SyntaxTrees.Where(ShouldAnalyzeSyntaxTree))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);

            foreach (var name in root.DescendantNodes().OfType<NameSyntax>())
            {
                MarkUsedBySymbol(assemblyNamesByPackageId, usedPackages, semanticModel.GetSymbolInfo(name, cancellationToken));
            }

            if (usedPackages.Count == assemblyNamesByPackageId.Count)
            {
                break;
            }
        }

        return usedPackages;
    }

    private static void MarkUsedBySymbol(
        IReadOnlyDictionary<string, IReadOnlySet<string>> assemblyNamesByPackageId,
        ISet<string> usedPackages,
        SymbolInfo symbolInfo)
    {
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        var assemblyName = GetAssemblyName(symbol);
        if (assemblyName is null)
        {
            return;
        }

        foreach (var entry in assemblyNamesByPackageId)
        {
            if (entry.Value.Contains(assemblyName))
            {
                usedPackages.Add(entry.Key);
            }
        }
    }

    private static string? GetAssemblyName(ISymbol? symbol)
    {
        return symbol switch
        {
            null => null,
            IAliasSymbol alias => GetAssemblyName(alias.Target),
            INamespaceSymbol namespaceSymbol => namespaceSymbol.ConstituentNamespaces
                .Select(candidate => candidate.ContainingAssembly?.Identity.Name)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)),
            _ => symbol.ContainingAssembly?.Identity.Name
        };
    }

    internal readonly record struct SemanticMetrics(
        int AbstractionLayerCount,
        int InterfacesWithSingleImplementation,
        double AverageMethodComplexity,
        int ExternalDependencyCount,
        int UnusedDependencyCount);
}
