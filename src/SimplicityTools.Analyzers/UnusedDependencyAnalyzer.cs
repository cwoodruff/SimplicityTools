using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnusedDependencyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF0002";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Package reference has no symbol usage",
        messageFormat: "PackageReference {0} has no detected symbol usage in C# source. Remove the dependency or justify it with source usage.",
        category: AnalyzerCategories.HalfRule,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0002/",
        description: "Package references that contribute no used symbols add restore time and maintenance overhead without value.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var packageReferences = PackageReferenceAnalysis.CollectPackageReferences(startContext.Options);
            if (packageReferences.Length == 0)
            {
                return;
            }

            var assembliesByPackage = PackageReferenceAnalysis.MapPackagesToAssemblies(startContext.Compilation);
            if (assembliesByPackage.Count == 0)
            {
                return;
            }

            var packageIdsByAssemblyIdentity = PackageReferenceAnalysis.BuildPackageIdsByAssemblyIdentity(assembliesByPackage);
            var usedPackages = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

            startContext.RegisterSemanticModelAction(semanticModelContext =>
            {
                var syntaxTree = semanticModelContext.SemanticModel.SyntaxTree;
                if (!AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxTree.FilePath))
                {
                    return;
                }

                PackageReferenceAnalysis.CollectUsedPackages(
                    semanticModelContext.SemanticModel,
                    packageIdsByAssemblyIdentity,
                    packageId => usedPackages.TryAdd(packageId, 0),
                    semanticModelContext.CancellationToken);
            });

            startContext.RegisterCompilationEndAction(endContext =>
                Report(endContext, packageReferences, assembliesByPackage, usedPackages));
        });
    }

    private static void Report(
        CompilationAnalysisContext context,
        ImmutableArray<PackageReferenceInfo> packageReferences,
        ImmutableDictionary<string, ImmutableArray<IAssemblySymbol>> assembliesByPackage,
        ConcurrentDictionary<string, byte> usedPackages)
    {
        var excludedPackages = AnalyzerOptionReader.GetNameSet(
            context.Options,
            context.Compilation.SyntaxTrees.FirstOrDefault(),
            AnalyzerOptionReader.ExcludedPackagesKey);
        foreach (var packageReference in packageReferences)
        {
            if (excludedPackages.Contains(packageReference.PackageId) ||
                !assembliesByPackage.ContainsKey(packageReference.NormalizedPackageId) ||
                usedPackages.ContainsKey(packageReference.NormalizedPackageId))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                packageReference.Location,
                properties: ImmutableDictionary<string, string?>.Empty.Add(PackageReferenceAnalysis.PackageIdPropertyName, packageReference.PackageId),
                messageArgs: [packageReference.PackageId]));
        }
    }
}
