using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace SimplicityTools.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleImplementationInterfaceCodeFixProvider))]
[Shared]
public sealed class SingleImplementationInterfaceCodeFixProvider : CodeFixProvider
{
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

        var title = $"Remove interface and use {concreteType.Name}";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => ApplyFixAsync(context.Document, interfaceSymbol, concreteType, cancellationToken),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Solution> ApplyFixAsync(
        Document document,
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol concreteType,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var updatedDocumentIds = new List<DocumentId>();
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Compilation was not available.");
        var interfaceMembers = CreateInterfaceMemberTemplates(
            compilation,
            interfaceSymbol,
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

                var rewriter = new InterfaceRemovalRewriter(semanticModel, interfaceSymbol, concreteType, interfaceMembers, cancellationToken);
                var rewrittenRoot = rewriter.Visit(syntaxRoot);
                if (rewrittenRoot is null || ReferenceEquals(rewrittenRoot, syntaxRoot))
                {
                    continue;
                }

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

            var formattedDocument = await Formatter.FormatAsync(updatedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            solution = formattedDocument.Project.Solution;
        }

        return solution;
    }

    private static ImmutableArray<InterfaceMemberTemplate> CreateInterfaceMemberTemplates(
        Compilation compilation,
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol concreteType,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<InterfaceMemberTemplate>();
        foreach (var member in interfaceSymbol.GetMembers().Where(static member => !member.IsImplicitlyDeclared))
        {
            foreach (var syntaxReference in member.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is not MemberDeclarationSyntax memberSyntax)
                {
                    continue;
                }

                var semanticModel = compilation.GetSemanticModel(memberSyntax.SyntaxTree);
                var rewriter = new SymbolReplacementRewriter(semanticModel, interfaceSymbol, concreteType, cancellationToken);
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

    private sealed class InterfaceRemovalRewriter(
        SemanticModel semanticModel,
        INamedTypeSymbol interfaceSymbol,
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
            => ReplaceName(node) ?? base.VisitGenericName(node)!;

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

        private NameSyntax? ReplaceName(NameSyntax node)
        {
            if (!MatchesSymbol(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol))
            {
                return null;
            }

            var replacement = concreteType.ToMinimalDisplayString(semanticModel, node.SpanStart);
            return SyntaxFactory.ParseName(replacement)
                .WithTriviaFrom(node);
        }

        private bool MatchesSymbol(ISymbol? symbol)
            => symbol is INamedTypeSymbol namedType &&
               SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, interfaceSymbol.OriginalDefinition);

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

    private sealed class SymbolReplacementRewriter(
        SemanticModel semanticModel,
        INamedTypeSymbol interfaceSymbol,
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
            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is not INamedTypeSymbol namedType ||
                !SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, interfaceSymbol.OriginalDefinition))
            {
                return null;
            }

            var replacement = concreteType.ToMinimalDisplayString(semanticModel, node.SpanStart);
            return SyntaxFactory.ParseName(replacement)
                .WithTriviaFrom(node);
        }
    }

    private sealed record InterfaceMemberTemplate(string Signature, MemberDeclarationSyntax MemberSyntax);
}
