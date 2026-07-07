using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Metrics;

internal sealed class SemanticCollectionPass
{
    public async Task<SemanticMetrics> CollectAsync(Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);

        // Keys are compilation-independent identities (assembly + fully-qualified name) so the
        // same interface or implementation seen from sibling-TFM compilations counts once and
        // cross-project matches survive metadata-reference fallback.
        var abstractionLayers = new HashSet<string>(StringComparer.Ordinal);
        var implementationMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var methodComplexities = new List<int>();
        var declaredPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in SelectOneTargetFrameworkPerProject(solution.Projects.Where(ShouldAnalyzeProject)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Could not build a Roslyn compilation for '{project.FilePath ?? project.Name}'.");

            var projectPackages = GetDeclaredPackageReferences(project.FilePath);
            declaredPackages.UnionWith(projectPackages);
            usedPackages.UnionWith(ResolveUsedPackages(compilation, projectPackages, cancellationToken));

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

    private static bool ShouldAnalyzeProject(Project project)
    {
        return !SourceFileConventions.IsTestProject(project.FilePath, project.Name);
    }

    /// <summary>
    /// A multi-targeted project appears in the workspace once per target framework; analyzing
    /// every instance double-counts each metric, so only the first instance is analyzed.
    /// </summary>
    internal static IEnumerable<Project> SelectOneTargetFrameworkPerProject(IEnumerable<Project> projects)
    {
        return projects
            .GroupBy(project => project.FilePath ?? project.Name, StringComparer.OrdinalIgnoreCase)
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

    private static IReadOnlySet<string> GetDeclaredPackageReferences(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var projectRoot = ProjectRootElement.Open(projectPath)
            ?? throw new InvalidOperationException($"Could not load project '{projectPath}'.");

        return projectRoot.Items
            .Where(item => string.Equals(item.ItemType, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Include)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlySet<string> ResolveUsedPackages(
        Compilation compilation,
        IReadOnlyCollection<string> declaredPackages,
        CancellationToken cancellationToken)
    {
        if (declaredPackages.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var packageLookup = BuildPackageLookup(compilation, declaredPackages);
        var usedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var syntaxTree in compilation.SyntaxTrees.Where(ShouldAnalyzeSyntaxTree))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);

            foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (usingDirective.Name is not null)
                {
                    MarkUsedByNamespace(packageLookup, usedPackages, usingDirective.Name.ToString());
                    MarkUsedBySymbol(packageLookup, usedPackages, semanticModel.GetSymbolInfo(usingDirective.Name, cancellationToken));
                }
            }

            foreach (var name in root.DescendantNodes().OfType<NameSyntax>())
            {
                MarkUsedByNamespace(packageLookup, usedPackages, name.ToString());
                MarkUsedBySymbol(packageLookup, usedPackages, semanticModel.GetSymbolInfo(name, cancellationToken));
            }

            if (usedPackages.Count == packageLookup.Count)
            {
                break;
            }
        }

        return usedPackages;
    }

    private static Dictionary<string, PackageUsageInfo> BuildPackageLookup(Compilation compilation, IReadOnlyCollection<string> declaredPackages)
    {
        var packageLookup = declaredPackages.ToDictionary(
            packageId => packageId,
            packageId => new PackageUsageInfo(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
        {
            if (string.IsNullOrWhiteSpace(reference.FilePath))
            {
                continue;
            }

            foreach (var packageId in declaredPackages)
            {
                if (!IsReferenceFromPackage(reference.FilePath, packageId))
                {
                    continue;
                }

                if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                {
                    continue;
                }

                packageLookup[packageId].AssemblyNames.Add(assemblySymbol.Identity.Name);
                CollectNamespaces(assemblySymbol.GlobalNamespace, packageLookup[packageId].Namespaces);
            }
        }

        return packageLookup;
    }

    private static bool IsReferenceFromPackage(string filePath, string packageId)
    {
        var normalizedPath = filePath.Replace('\\', '/');
        var normalizedPackageId = packageId.ToLowerInvariant();
        return normalizedPath.Contains($"/.nuget/packages/{normalizedPackageId}/", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains($"/packages/{normalizedPackageId}/", StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectNamespaces(INamespaceSymbol namespaceSymbol, ISet<string> namespaces)
    {
        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            if (!nestedNamespace.IsGlobalNamespace)
            {
                namespaces.Add(nestedNamespace.ToDisplayString());
            }

            CollectNamespaces(nestedNamespace, namespaces);
        }
    }

    private static void MarkUsedByNamespace(
        IReadOnlyDictionary<string, PackageUsageInfo> packageLookup,
        ISet<string> usedPackages,
        string namespaceText)
    {
        if (string.IsNullOrWhiteSpace(namespaceText))
        {
            return;
        }

        var normalizedNamespace = namespaceText.Replace("global::", string.Empty, StringComparison.Ordinal);
        foreach (var entry in packageLookup)
        {
            if (entry.Value.Namespaces.Any(packageNamespace =>
                    string.Equals(packageNamespace, normalizedNamespace, StringComparison.Ordinal) ||
                    packageNamespace.StartsWith(normalizedNamespace + ".", StringComparison.Ordinal) ||
                    normalizedNamespace.StartsWith(packageNamespace + ".", StringComparison.Ordinal)))
            {
                usedPackages.Add(entry.Key);
            }
        }
    }

    private static void MarkUsedBySymbol(
        IReadOnlyDictionary<string, PackageUsageInfo> packageLookup,
        ISet<string> usedPackages,
        SymbolInfo symbolInfo)
    {
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        var assemblyName = GetAssemblyName(symbol);
        if (assemblyName is null)
        {
            return;
        }

        foreach (var entry in packageLookup)
        {
            if (entry.Value.AssemblyNames.Contains(assemblyName))
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

    private sealed class PackageUsageInfo
    {
        public HashSet<string> AssemblyNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> Namespaces { get; } = new(StringComparer.Ordinal);
    }
}
