using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace SimplicityTools.Metrics;

internal sealed class HeuristicCollectionPass
{
    public async Task<int> CollectPrimaryPathFileCountAsync(Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);

        var documents = new List<DocumentHeuristicInfo>();
        foreach (var project in solution.Projects.Where(ShouldAnalyzeProject))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var document in project.Documents.Where(ShouldAnalyzeDocument))
            {
                cancellationToken.ThrowIfCancellationRequested();
                documents.Add(await AnalyzeDocumentAsync(document, solution, cancellationToken).ConfigureAwait(false));
            }
        }

        var annotatedDocuments = documents.Count(document => document.HasPrimaryPathAnnotation);
        if (annotatedDocuments > 0)
        {
            return annotatedDocuments;
        }

        var conventionMatches = documents.Count(document => document.MatchesPrimaryPathConvention);
        var percentileCandidates = documents
            .Where(document => !document.MatchesPrimaryPathConvention)
            .ToArray();

        var percentileThreshold = GetTopQuartileThreshold(percentileCandidates.Select(document => document.InboundReferenceCount).ToArray());
        if (percentileThreshold is null)
        {
            return conventionMatches;
        }

        return conventionMatches + percentileCandidates.Count(document => document.InboundReferenceCount >= percentileThreshold.Value);
    }

    private static bool ShouldAnalyzeProject(Project project)
    {
        return !SourceFileConventions.IsTestProject(project.FilePath, project.Name);
    }

    private static bool ShouldAnalyzeDocument(Document document)
    {
        return !string.IsNullOrWhiteSpace(document.FilePath) && SourceFileConventions.IsCountableSourceFile(document.FilePath);
    }

    private static async Task<DocumentHeuristicInfo> AnalyzeDocumentAsync(
        Document document,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var filePath = document.FilePath
            ?? throw new InvalidOperationException($"Document '{document.Name}' does not have a file path.");
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not load syntax for '{filePath}'.");
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not build a semantic model for '{filePath}'.");

        var hasPrimaryPathAnnotation = HasPrimaryPathAnnotation(syntaxRoot, semanticModel, cancellationToken);
        var inboundReferenceCount = await CountInboundReferencesAsync(
                CollectNamedTypes(syntaxRoot, semanticModel),
                document,
                solution,
                cancellationToken)
            .ConfigureAwait(false);

        return new DocumentHeuristicInfo(
            hasPrimaryPathAnnotation,
            MatchesPrimaryPathConvention(filePath),
            inboundReferenceCount);
    }

    private static bool HasPrimaryPathAnnotation(SyntaxNode syntaxRoot, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        foreach (var attribute in syntaxRoot.DescendantNodes().OfType<AttributeSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var constructorSymbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol as IMethodSymbol;
            var attributeType = constructorSymbol?.ContainingType;
            if (attributeType?.ToDisplayString() == "SimplicityTools.Metrics.PrimaryPathAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyCollection<INamedTypeSymbol> CollectNamedTypes(SyntaxNode syntaxRoot, SemanticModel semanticModel)
    {
        var symbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var declaration in syntaxRoot.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(declaration) is INamedTypeSymbol symbol)
            {
                symbols.Add(symbol);
            }
        }

        return symbols;
    }

    private static async Task<int> CountInboundReferencesAsync(
        IReadOnlyCollection<INamedTypeSymbol> symbols,
        Document document,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var symbol in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    var referenceDocument = location.Document;
                    if (referenceDocument.Id == document.Id ||
                        string.IsNullOrWhiteSpace(referenceDocument.FilePath) ||
                        !SourceFileConventions.IsCountableSourceFile(referenceDocument.FilePath) ||
                        SourceFileConventions.IsTestProject(referenceDocument.Project.FilePath, referenceDocument.Project.Name))
                    {
                        continue;
                    }

                    count++;
                }
            }
        }

        return count;
    }

    private static bool MatchesPrimaryPathConvention(string filePath)
    {
        return SourceFileConventions.ContainsDirectorySegment(filePath, "Controllers") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Endpoints") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Handlers") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Pages");
    }

    private static int? GetTopQuartileThreshold(IReadOnlyList<int> counts)
    {
        if (counts.Count == 0)
        {
            return null;
        }

        var sortedCounts = counts.OrderBy(count => count).ToArray();
        var thresholdIndex = (int)Math.Ceiling(sortedCounts.Length * 0.75d) - 1;
        thresholdIndex = Math.Clamp(thresholdIndex, 0, sortedCounts.Length - 1);

        var threshold = sortedCounts[thresholdIndex];
        return threshold > 0 ? threshold : null;
    }

    private readonly record struct DocumentHeuristicInfo(
        bool HasPrimaryPathAnnotation,
        bool MatchesPrimaryPathConvention,
        int InboundReferenceCount);
}
