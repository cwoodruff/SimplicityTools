using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static startContext =>
        {
            var methodDeclarations = new ConcurrentDictionary<IMethodSymbol, MethodDeclarationSyntax>(SymbolEqualityComparer.Default);
            var invocationTargets = new ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<IMethodSymbol, byte>>(SymbolEqualityComparer.Default);

            startContext.RegisterSymbolAction(
                symbolContext => CollectMethodDeclaration(symbolContext, methodDeclarations),
                SymbolKind.Method);
            startContext.RegisterOperationAction(
                operationContext => CollectInvocation(operationContext, invocationTargets),
                OperationKind.Invocation);
            startContext.RegisterCompilationEndAction(
                endContext => Report(endContext, methodDeclarations, invocationTargets));
        });
    }

    private static void CollectMethodDeclaration(
        SymbolAnalysisContext context,
        ConcurrentDictionary<IMethodSymbol, MethodDeclarationSyntax> methodDeclarations)
    {
        if (context.Symbol is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        MethodDeclarationSyntax? declaration = null;
        foreach (var syntaxReference in method.DeclaringSyntaxReferences)
        {
            if (AnalyzerSourceFileConventions.IsCountableSourceFile(syntaxReference.SyntaxTree.FilePath) &&
                syntaxReference.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax candidate)
            {
                declaration = candidate;
            }
        }

        if (declaration is not null)
        {
            methodDeclarations[method.OriginalDefinition] = declaration;
        }
    }

    private static void CollectInvocation(
        OperationAnalysisContext context,
        ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<IMethodSymbol, byte>> invocationTargets)
    {
        if (!AnalyzerSourceFileConventions.IsCountableSourceFile(context.Operation.Syntax.SyntaxTree.FilePath))
        {
            return;
        }

        // Attribute invocations inside lambdas and local functions to the enclosing ordinary
        // method, matching the previous per-declaration syntax walk.
        var containingSymbol = context.ContainingSymbol;
        while (containingSymbol is IMethodSymbol nested &&
               nested.MethodKind is MethodKind.LocalFunction or MethodKind.AnonymousFunction)
        {
            containingSymbol = nested.ContainingSymbol;
        }

        if (containingSymbol is not IMethodSymbol caller ||
            caller.MethodKind != MethodKind.Ordinary ||
            ((IInvocationOperation)context.Operation).TargetMethod is not { } invokedMethod)
        {
            return;
        }

        var target = (invokedMethod.ReducedFrom ?? invokedMethod).OriginalDefinition;
        invocationTargets
            .GetOrAdd(caller.OriginalDefinition, static _ => new ConcurrentDictionary<IMethodSymbol, byte>(SymbolEqualityComparer.Default))
            .TryAdd(target, 0);
    }

    private static void Report(
        CompilationAnalysisContext context,
        ConcurrentDictionary<IMethodSymbol, MethodDeclarationSyntax> methodDeclarations,
        ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<IMethodSymbol, byte>> invocationTargets)
    {
        if (methodDeclarations.IsEmpty)
        {
            return;
        }

        var sourceMethods = methodDeclarations.Keys.ToImmutableHashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var uniqueDispatchTargets = BuildUniqueDispatchTargets(sourceMethods);
        var callGraph = BuildCallGraph(sourceMethods, uniqueDispatchTargets, invocationTargets, context.CancellationToken);
        var memo = new Dictionary<IMethodSymbol, int>(SymbolEqualityComparer.Default);

        foreach (var method in sourceMethods)
        {
            var depth = ComputeDepth(method, callGraph, memo, [], context.CancellationToken);
            if (!methodDeclarations.TryGetValue(method, out var declaration))
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
        ImmutableHashSet<IMethodSymbol> sourceMethods,
        ImmutableDictionary<IMethodSymbol, IMethodSymbol> uniqueDispatchTargets,
        ConcurrentDictionary<IMethodSymbol, ConcurrentDictionary<IMethodSymbol, byte>> invocationTargets,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<IMethodSymbol, ImmutableArray<IMethodSymbol>>(SymbolEqualityComparer.Default);

        foreach (var method in sourceMethods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var callees = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            if (invocationTargets.TryGetValue(method, out var targets))
            {
                foreach (var target in targets.Keys)
                {
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
            }

            builder[method] = ImmutableArray.CreateRange(callees);
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
