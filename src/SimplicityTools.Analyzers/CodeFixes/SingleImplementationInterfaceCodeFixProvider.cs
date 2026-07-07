using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace SimplicityTools.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleImplementationInterfaceCodeFixProvider))]
[Shared]
public sealed class SingleImplementationInterfaceCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Marks generic names whose type arguments contained both the interface and its single
    /// implementation (typical DI registrations such as AddScoped&lt;IFoo, Foo&gt;()), so the
    /// rewritten call site receives a review comment instead of a silent semantic change.
    /// </summary>
    private static readonly SyntaxAnnotation DependencyInjectionReviewAnnotation = new("SimplicityTools.SF0001.DependencyInjectionReview");

    public override ImmutableArray<string> FixableDiagnosticIds => [SingleImplementationInterfaceAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (context.Document is null)
        {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var interfaceDeclaration = node.FirstAncestorOrSelf<InterfaceDeclarationSyntax>();
        if (interfaceDeclaration is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(interfaceDeclaration, context.CancellationToken) is not INamedTypeSymbol interfaceSymbol)
        {
            return;
        }

        // The analyzer passes the single implementation through Diagnostic.Properties so the
        // code fix never has to re-index the compilation to find it.
        if (!diagnostic.Properties.TryGetValue(SingleImplementationInterfaceAnalyzer.ImplementationIdPropertyName, out var implementationId) ||
            implementationId is null ||
            string.IsNullOrWhiteSpace(implementationId))
        {
            return;
        }

        var compilation = semanticModel.Compilation;
        if (DocumentationCommentId.GetFirstSymbolForDeclarationId(implementationId, compilation) is not INamedTypeSymbol concreteType)
        {
            return;
        }

        // Rewriting references to a less accessible implementation produces CS0050-family
        // errors (public members exposing an internal type), so the fix is not offered.
        if (SymbolVisibility.IsExternallyVisible(interfaceSymbol) && !SymbolVisibility.IsExternallyVisible(concreteType))
        {
            return;
        }

        if (SymbolIdentity.Create(interfaceSymbol) is not { } interfaceIdentity ||
            SymbolIdentity.Create(concreteType) is not { } concreteIdentity)
        {
            return;
        }

        // nameof(IFoo) sites would either silently change a string value or stop compiling
        // once the interface is removed; the fix is not offered while any exist.
        if (await HasNameofReferenceAsync(context.Document.Project.Solution, interfaceIdentity, interfaceSymbol.Name, context.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var title = $"Remove interface and use {concreteType.Name}";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => ApplyFixAsync(context.Document, interfaceIdentity, concreteIdentity, concreteType, cancellationToken),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<bool> HasNameofReferenceAsync(
        Solution solution,
        SymbolIdentity interfaceIdentity,
        string interfaceName,
        CancellationToken cancellationToken)
    {
        foreach (var project in solution.Projects.Where(static project => project.Language == LanguageNames.CSharp))
        {
            foreach (var document in project.Documents)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var content = text.ToString();
                if (!content.Contains("nameof") || !content.Contains(interfaceName))
                {
                    continue;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (root is null || semanticModel is null)
                {
                    continue;
                }

                foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not IdentifierNameSyntax { Identifier.ValueText: "nameof" } ||
                        invocation.ArgumentList.Arguments.Count != 1)
                    {
                        continue;
                    }

                    var argument = invocation.ArgumentList.Arguments[0].Expression;
                    foreach (var name in argument.DescendantNodesAndSelf().OfType<NameSyntax>())
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
                        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                        if (interfaceIdentity.Matches(symbol))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static async Task<Solution> ApplyFixAsync(
        Document document,
        SymbolIdentity interfaceIdentity,
        SymbolIdentity concreteIdentity,
        INamedTypeSymbol concreteType,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var updatedDocumentIds = new List<DocumentId>();
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Compilation was not available.");
        var interfaceMembers = CreateInterfaceMemberTemplates(
            compilation,
            interfaceIdentity,
            concreteType,
            cancellationToken);

        foreach (var project in solution.Projects.Where(static project => project.Language == LanguageNames.CSharp))
        {
            foreach (var candidateDocument in project.Documents)
            {
                var syntaxRoot = await candidateDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null)
                {
                    continue;
                }

                var semanticModel = await candidateDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel is null)
                {
                    continue;
                }

                var rewriter = new InterfaceRemovalRewriter(semanticModel, interfaceIdentity, concreteIdentity, concreteType, interfaceMembers, cancellationToken);
                var rewrittenRoot = rewriter.Visit(syntaxRoot);
                if (rewrittenRoot is null || ReferenceEquals(rewrittenRoot, syntaxRoot))
                {
                    continue;
                }

                rewrittenRoot = AddDependencyInjectionReviewComments(rewrittenRoot, interfaceIdentity.Name, concreteType.Name);
                solution = solution.WithDocumentSyntaxRoot(candidateDocument.Id, rewrittenRoot.WithAdditionalAnnotations(Formatter.Annotation));
                updatedDocumentIds.Add(candidateDocument.Id);
            }
        }

        foreach (var documentId in updatedDocumentIds.Distinct())
        {
            var updatedDocument = solution.GetDocument(documentId);
            if (updatedDocument is null)
            {
                continue;
            }

            var simplifiedDocument = await Simplifier.ReduceAsync(updatedDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var formattedDocument = await Formatter.FormatAsync(simplifiedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            solution = formattedDocument.Project.Solution;
        }

        return solution;
    }

    private static SyntaxNode AddDependencyInjectionReviewComments(SyntaxNode root, string interfaceName, string concreteTypeName)
    {
        var targets = root.GetAnnotatedNodes(DependencyInjectionReviewAnnotation)
            .Select(static node => (SyntaxNode?)node.FirstAncestorOrSelf<StatementSyntax>() ?? node.FirstAncestorOrSelf<MemberDeclarationSyntax>())
            .OfType<SyntaxNode>()
            .Distinct()
            .ToArray();
        if (targets.Length == 0)
        {
            return root;
        }

        var comment = SyntaxFactory.Comment($"// TODO: review DI registration: interface '{interfaceName}' was replaced with '{concreteTypeName}'.");
        return root.ReplaceNodes(targets, (_, current) =>
        {
            var leadingTrivia = current.GetLeadingTrivia();
            var indentation = leadingTrivia.LastOrDefault(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));
            var updatedTrivia = leadingTrivia
                .Add(comment)
                .Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
            if (indentation.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                updatedTrivia = updatedTrivia.Add(indentation);
            }

            return current.WithLeadingTrivia(updatedTrivia);
        });
    }

    private static ImmutableArray<InterfaceMemberTemplate> CreateInterfaceMemberTemplates(
        Compilation compilation,
        SymbolIdentity interfaceIdentity,
        INamedTypeSymbol concreteType,
        CancellationToken cancellationToken)
    {
        if (DocumentationCommentId.GetFirstSymbolForDeclarationId(interfaceIdentity.DeclarationId, compilation) is not INamedTypeSymbol interfaceSymbol)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<InterfaceMemberTemplate>();
        foreach (var member in interfaceSymbol.GetMembers().Where(static member => !member.IsImplicitlyDeclared))
        {
            foreach (var syntaxReference in member.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is not MemberDeclarationSyntax memberSyntax)
                {
                    continue;
                }

                // Members can be hoisted into dependent interfaces declared in other files (or
                // projects), where the source file's using directives are unavailable. Render
                // every type reference fully qualified and let Simplifier shorten whatever the
                // destination file can resolve.
                var semanticModel = compilation.GetSemanticModel(memberSyntax.SyntaxTree);
                var rewriter = new FullyQualifyingMemberRewriter(semanticModel, interfaceIdentity, concreteType, cancellationToken);
                var rewrittenSyntax = (MemberDeclarationSyntax)(rewriter.Visit(memberSyntax) ?? memberSyntax);
                builder.Add(new InterfaceMemberTemplate(CreateSignature(member), rewrittenSyntax.WithoutTrivia()));
            }
        }

        return builder.ToImmutable();
    }

    private static string CreateSignature(ISymbol member)
        => member switch
        {
            IMethodSymbol method => $"method:{method.MethodKind}:{method.Name}`{method.Arity}({string.Join(",", method.Parameters.Select(CreateParameterSignature))})->{CreateTypeSignature(method.ReturnType)}",
            IPropertySymbol property => $"property:{(property.IsIndexer ? "this" : property.Name)}({string.Join(",", property.Parameters.Select(CreateParameterSignature))})->{CreateTypeSignature(property.Type)}",
            IEventSymbol eventSymbol => $"event:{eventSymbol.Name}->{CreateTypeSignature(eventSymbol.Type)}",
            _ => $"{member.Kind}:{member.MetadataName}"
        };

    private static string CreateParameterSignature(IParameterSymbol parameter)
        => $"{parameter.RefKind}:{CreateTypeSignature(parameter.Type)}";

    private static string CreateTypeSignature(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static NameSyntax CreateConcreteTypeName(INamedTypeSymbol concreteType, NameSyntax original)
    {
        // Positions that grammatically require a simple name (the right side of a qualified
        // name or member access) cannot hold a fully qualified replacement.
        var requiresSimpleName =
            (original.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Right == original) ||
            (original.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == original) ||
            (original.Parent is AliasQualifiedNameSyntax aliasQualifiedName && aliasQualifiedName.Name == original);
        var replacement = requiresSimpleName
            ? SyntaxFactory.IdentifierName(concreteType.Name)
            : SyntaxFactory.ParseName(concreteType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        return replacement
            .WithTriviaFrom(original)
            .WithAdditionalAnnotations(Simplifier.Annotation);
    }

    /// <summary>
    /// Compilation-independent identity for a named type. Cross-project rewriting sees the
    /// interface and implementation through other compilations (including retargeted or
    /// metadata-resolved symbols), where <see cref="SymbolEqualityComparer.Default"/> silently
    /// fails; the documentation-comment declaration id plus assembly name matches everywhere.
    /// </summary>
    private sealed record SymbolIdentity(string DeclarationId, string AssemblyName, string Name)
    {
        public static SymbolIdentity? Create(INamedTypeSymbol symbol)
        {
            var declarationId = DocumentationCommentId.CreateDeclarationId(symbol.OriginalDefinition);
            var assemblyName = symbol.ContainingAssembly?.Identity.Name;
            return declarationId is null || assemblyName is null
                ? null
                : new SymbolIdentity(declarationId, assemblyName, symbol.Name);
        }

        public bool Matches(ISymbol? symbol)
        {
            if (symbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var definition = namedType.OriginalDefinition;
            return string.Equals(definition.Name, Name, StringComparison.Ordinal) &&
                   string.Equals(definition.ContainingAssembly?.Identity.Name, AssemblyName, StringComparison.Ordinal) &&
                   string.Equals(DocumentationCommentId.CreateDeclarationId(definition), DeclarationId, StringComparison.Ordinal);
        }
    }

    private sealed class InterfaceRemovalRewriter(
        SemanticModel semanticModel,
        SymbolIdentity interfaceIdentity,
        SymbolIdentity concreteIdentity,
        INamedTypeSymbol concreteType,
        ImmutableArray<InterfaceMemberTemplate> interfaceMembers,
        CancellationToken cancellationToken) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (MatchesSymbol(declaredSymbol))
            {
                return null;
            }

            var rewrittenNode = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node)!;
            if (!DirectlyInheritsInterface(node) ||
                declaredSymbol is not INamedTypeSymbol dependentInterfaceSymbol)
            {
                return rewrittenNode;
            }

            var availableSignatures = GetAvailableSignatures(dependentInterfaceSymbol);
            var additionalMembers = interfaceMembers
                .Where(template => availableSignatures.Add(template.Signature))
                .Select(static template => template.MemberSyntax)
                .ToArray();

            return additionalMembers.Length == 0
                ? rewrittenNode
                : rewrittenNode.WithMembers(rewrittenNode.Members.AddRange(additionalMembers));
        }

        public override SyntaxNode? VisitBaseList(BaseListSyntax node)
        {
            var filteredTypes = node.Types
                .Where(baseType => !MatchesSymbol(semanticModel.GetSymbolInfo(baseType.Type, cancellationToken).Symbol))
                .Select(baseType => (BaseTypeSyntax?)Visit(baseType))
                .Where(static baseType => baseType is not null)
                .Cast<BaseTypeSyntax>()
                .ToList();

            return filteredTypes.Count == 0
                ? null
                : node.WithTypes(SyntaxFactory.SeparatedList(filteredTypes));
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            => ReplaceName(node) ?? base.VisitIdentifierName(node)!;

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var requiresReview = RequiresDependencyInjectionReview(node);
            var rewrittenNode = ReplaceName(node) ?? base.VisitGenericName(node)!;
            return requiresReview ? rewrittenNode.WithAdditionalAnnotations(DependencyInjectionReviewAnnotation) : rewrittenNode;
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            => ReplaceName(node) ?? base.VisitQualifiedName(node)!;

        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            => ReplaceName(node) ?? base.VisitAliasQualifiedName(node)!;

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var matchesInterface = MatchesExplicitInterfaceSpecifier(node.ExplicitInterfaceSpecifier);
            var rewrittenNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            return matchesInterface ? EnsurePublic(rewrittenNode.WithExplicitInterfaceSpecifier(null)) : rewrittenNode;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var matchesInterface = MatchesExplicitInterfaceSpecifier(node.ExplicitInterfaceSpecifier);
            var rewrittenNode = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
            return matchesInterface ? EnsurePublic(rewrittenNode.WithExplicitInterfaceSpecifier(null)) : rewrittenNode;
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            var matchesInterface = MatchesExplicitInterfaceSpecifier(node.ExplicitInterfaceSpecifier);
            var rewrittenNode = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!;
            return matchesInterface ? EnsurePublic(rewrittenNode.WithExplicitInterfaceSpecifier(null)) : rewrittenNode;
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var matchesInterface = MatchesExplicitInterfaceSpecifier(node.ExplicitInterfaceSpecifier);
            var rewrittenNode = (EventDeclarationSyntax)base.VisitEventDeclaration(node)!;
            return matchesInterface ? EnsurePublic(rewrittenNode.WithExplicitInterfaceSpecifier(null)) : rewrittenNode;
        }

        private bool MatchesExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax? specifier)
            => specifier is not null && MatchesSymbol(semanticModel.GetSymbolInfo(specifier.Name, cancellationToken).Symbol);

        private bool DirectlyInheritsInterface(InterfaceDeclarationSyntax node)
            => node.BaseList?.Types.Any(baseType => MatchesSymbol(semanticModel.GetSymbolInfo(baseType.Type, cancellationToken).Symbol)) == true;

        private bool RequiresDependencyInjectionReview(GenericNameSyntax node)
        {
            if (node.TypeArgumentList.Arguments.Count < 2)
            {
                return false;
            }

            var referencesInterface = false;
            var referencesImplementation = false;
            foreach (var typeArgument in node.TypeArgumentList.Arguments)
            {
                var symbol = semanticModel.GetSymbolInfo(typeArgument, cancellationToken).Symbol;
                if (interfaceIdentity.Matches(symbol))
                {
                    referencesInterface = true;
                }
                else if (concreteIdentity.Matches(symbol))
                {
                    referencesImplementation = true;
                }
            }

            return referencesInterface && referencesImplementation;
        }

        private NameSyntax? ReplaceName(NameSyntax node)
            => MatchesSymbol(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol)
                ? CreateConcreteTypeName(concreteType, node)
                : null;

        private bool MatchesSymbol(ISymbol? symbol) => interfaceIdentity.Matches(symbol);

        private HashSet<string> GetAvailableSignatures(INamedTypeSymbol dependentInterfaceSymbol)
        {
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            AddSignatures(signatures, dependentInterfaceSymbol.GetMembers());

            foreach (var baseInterface in dependentInterfaceSymbol.Interfaces.Where(baseInterface => !MatchesSymbol(baseInterface)))
            {
                AddSignatures(signatures, baseInterface.GetMembers());
                foreach (var inheritedInterface in baseInterface.AllInterfaces)
                {
                    AddSignatures(signatures, inheritedInterface.GetMembers());
                }
            }

            return signatures;
        }

        private static void AddSignatures(HashSet<string> signatures, IEnumerable<ISymbol> members)
        {
            foreach (var member in members.Where(static member => !member.IsImplicitlyDeclared))
            {
                signatures.Add(CreateSignature(member));
            }
        }

        private static MethodDeclarationSyntax EnsurePublic(MethodDeclarationSyntax node)
            => HasAccessibility(node.Modifiers) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        private static PropertyDeclarationSyntax EnsurePublic(PropertyDeclarationSyntax node)
            => HasAccessibility(node.Modifiers) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        private static IndexerDeclarationSyntax EnsurePublic(IndexerDeclarationSyntax node)
            => HasAccessibility(node.Modifiers) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        private static EventDeclarationSyntax EnsurePublic(EventDeclarationSyntax node)
            => HasAccessibility(node.Modifiers) ? node : node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        private static bool HasAccessibility(SyntaxTokenList modifiers)
            => modifiers.Any(token => token.IsKind(SyntaxKind.PublicKeyword) ||
                                      token.IsKind(SyntaxKind.InternalKeyword) ||
                                      token.IsKind(SyntaxKind.ProtectedKeyword) ||
                                      token.IsKind(SyntaxKind.PrivateKeyword));
    }

    private sealed class FullyQualifyingMemberRewriter(
        SemanticModel semanticModel,
        SymbolIdentity interfaceIdentity,
        INamedTypeSymbol concreteType,
        CancellationToken cancellationToken) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            => ReplaceName(node) ?? base.VisitIdentifierName(node)!;

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            => ReplaceName(node) ?? base.VisitGenericName(node)!;

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
            => ReplaceName(node) ?? base.VisitQualifiedName(node)!;

        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            => ReplaceName(node) ?? base.VisitAliasQualifiedName(node)!;

        private NameSyntax? ReplaceName(NameSyntax node)
        {
            if (node.IsVar)
            {
                return null;
            }

            var symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
            if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } attributeConstructor)
            {
                symbol = attributeConstructor.ContainingType;
            }

            if (symbol is not ITypeSymbol type || type is ITypeParameterSymbol or IDynamicTypeSymbol)
            {
                return null;
            }

            if (interfaceIdentity.Matches(type))
            {
                return CreateConcreteTypeName(concreteType, node);
            }

            var mappedType = MapType(type);
            return SyntaxFactory.ParseName(mappedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .WithTriviaFrom(node)
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private ITypeSymbol MapType(ITypeSymbol type)
        {
            if (interfaceIdentity.Matches(type))
            {
                return concreteType;
            }

            switch (type)
            {
                case INamedTypeSymbol { IsGenericType: true } namedType:
                    var mappedArguments = namedType.TypeArguments.Select(MapType).ToArray();
                    return mappedArguments.Zip(namedType.TypeArguments, static (mapped, original) => ReferenceEquals(mapped, original)).All(static unchanged => unchanged)
                        ? namedType
                        : namedType.ConstructedFrom.Construct(mappedArguments);
                case IArrayTypeSymbol arrayType:
                    var mappedElement = MapType(arrayType.ElementType);
                    return ReferenceEquals(mappedElement, arrayType.ElementType)
                        ? arrayType
                        : semanticModel.Compilation.CreateArrayTypeSymbol(mappedElement, arrayType.Rank);
                default:
                    return type;
            }
        }
    }

    private sealed record InterfaceMemberTemplate(string Signature, MemberDeclarationSyntax MemberSyntax);
}
