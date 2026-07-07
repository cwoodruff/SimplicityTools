using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AbstractionLayerDepthAnalyzer : DiagnosticAnalyzer
{
    internal const int DefaultLayerThreshold = 8;

    public const string DiagnosticId = "SF0004";

    public static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Method call chain is too deep",
        messageFormat: "Method {0} passes through {1} abstraction layers, exceeding the limit of {2}",
        category: AnalyzerCategories.PrimaryPathFirst,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: "https://simplicitytools.dev/analyzers/sf0004/",
        description: "Long source-level call chains hide the primary path behind wrappers and indirection.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    private static void Analyze(CompilationAnalysisContext context)
    {
        var sourceIndex = SourceSymbolIndex.Create(context.Compilation, context.CancellationToken);
        if (sourceIndex.Methods.IsDefaultOrEmpty)
        {
            return;
        }

        var sourceMethods = sourceIndex.Methods.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var uniqueDispatchTargets = BuildUniqueDispatchTargets(sourceMethods);
        var callGraph = BuildCallGraph(context.Compilation, sourceIndex, sourceMethods, uniqueDispatchTargets, context.CancellationToken);
        var memo = new Dictionary<IMethodSymbol, int>(SymbolEqualityComparer.Default);

        foreach (var method in sourceMethods)
        {
            var depth = ComputeDepth(method, callGraph, memo, [], context.CancellationToken);
            if (!sourceIndex.TryGetMethodDeclaration(method, out var declaration))
            {
                continue;
            }

            var threshold = AnalyzerOptionReader.GetThreshold(
                context.Options,
                declaration.SyntaxTree,
                AnalyzerOptionReader.LayerThresholdKey,
                DefaultLayerThreshold);
            if (depth <= threshold)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                declaration.Identifier.GetLocation(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                depth,
                threshold));
        }
    }

    private static ImmutableDictionary<IMethodSymbol, IMethodSymbol> BuildUniqueDispatchTargets(ImmutableHashSet<IMethodSymbol> sourceMethods)
    {
        var buckets = new Dictionary<IMethodSymbol, HashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var method in sourceMethods.Where(static method => !method.IsAbstract && !method.ContainingType.IsAbstract))
        {
            foreach (var interfaceMember in method.ContainingType.AllInterfaces
                         .SelectMany(static @interface => @interface.GetMembers().OfType<IMethodSymbol>()))
            {
                var implementation = method.ContainingType.FindImplementationForInterfaceMember(interfaceMember) as IMethodSymbol;
                if (!SymbolEqualityComparer.Default.Equals(implementation?.OriginalDefinition, method))
                {
                    continue;
                }

                AddBucketValue(buckets, interfaceMember.OriginalDefinition, method);
            }

            if (method.OverriddenMethod is not null)
            {
                AddBucketValue(buckets, method.OverriddenMethod.OriginalDefinition, method);
            }
        }

        var builder = ImmutableDictionary.CreateBuilder<IMethodSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var pair in buckets.Where(static pair => pair.Value.Count == 1))
        {
            builder[pair.Key] = pair.Value.Single();
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<IMethodSymbol, ImmutableArray<IMethodSymbol>> BuildCallGraph(
        Compilation compilation,
        SourceSymbolIndex sourceIndex,
        ImmutableHashSet<IMethodSymbol> sourceMethods,
        ImmutableDictionary<IMethodSymbol, IMethodSymbol> uniqueDispatchTargets,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<IMethodSymbol, ImmutableArray<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var method in sourceMethods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!sourceIndex.TryGetMethodDeclaration(method, out var declaration))
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
            var callees = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol invokedMethod)
                {
                    continue;
                }

                var target = (invokedMethod.ReducedFrom ?? invokedMethod).OriginalDefinition;
                if (sourceMethods.Contains(target))
                {
                    callees.Add(target);
                    continue;
                }

                if (uniqueDispatchTargets.TryGetValue(target, out var uniqueDispatchTarget))
                {
                    callees.Add(uniqueDispatchTarget);
                }
            }

            builder[method] = [.. callees];
        }

        return builder.ToImmutable();
    }

    private static int ComputeDepth(
        IMethodSymbol method,
        ImmutableDictionary<IMethodSymbol, ImmutableArray<IMethodSymbol>> callGraph,
        Dictionary<IMethodSymbol, int> memo,
        HashSet<IMethodSymbol> activePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (memo.TryGetValue(method, out var cachedDepth))
        {
            return cachedDepth;
        }

        if (!activePath.Add(method))
        {
            return 0;
        }

        var maxDepth = 0;
        if (callGraph.TryGetValue(method, out var callees))
        {
            foreach (var callee in callees)
            {
                maxDepth = Math.Max(maxDepth, 1 + ComputeDepth(callee, callGraph, memo, activePath, cancellationToken));
            }
        }

        activePath.Remove(method);
        memo[method] = maxDepth;
        return maxDepth;
    }

    private static void AddBucketValue(
        IDictionary<IMethodSymbol, HashSet<IMethodSymbol>> buckets,
        IMethodSymbol key,
        IMethodSymbol value)
    {
        if (!buckets.TryGetValue(key, out var methods))
        {
            methods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            buckets[key] = methods;
        }

        methods.Add(value);
    }
}
