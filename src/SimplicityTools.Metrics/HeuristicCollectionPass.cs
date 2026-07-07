using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Metrics;

internal sealed class HeuristicCollectionPass
{
    public async Task<HeuristicMetrics> CollectAsync(Solution solution, IReadOnlySet<string> projectFilePaths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(projectFilePaths);

        var analyzableDocuments = SelectAnalyzableDocuments(solution, projectFilePaths, cancellationToken);

        // Every analyzable document participates: as a definition site accumulating inbound
        // references and as a reference site contributing them. One syntax walk per document
        // replaces the previous per-type SymbolFinder.FindReferencesAsync sweep, which searched
        // the whole solution once for every named type and was quadratic in practice.
        var inboundReferenceCounts = analyzableDocuments
            .ToDictionary(document => document.FilePath!, _ => 0, StringComparer.OrdinalIgnoreCase);

        var documents = new List<DocumentHeuristicInfo>(analyzableDocuments.Count);
        foreach (var document in analyzableDocuments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            documents.Add(await AnalyzeDocumentAsync(document, inboundReferenceCounts, cancellationToken).ConfigureAwait(false));
        }

        return new HeuristicMetrics(CountPrimaryPathFiles(documents, inboundReferenceCounts), documents.Count);
    }

    private static List<Document> SelectAnalyzableDocuments(
        Solution solution,
        IReadOnlySet<string> projectFilePaths,
        CancellationToken cancellationToken)
    {
        var analyzableDocuments = new List<Document>();
        foreach (var project in SemanticCollectionPass.SelectAnalyzableProjects(solution, projectFilePaths))
        {
            cancellationToken.ThrowIfCancellationRequested();
            analyzableDocuments.AddRange(project.Documents.Where(ShouldAnalyzeDocument));
        }

        return analyzableDocuments;
    }

    private static int CountPrimaryPathFiles(
        IReadOnlyList<DocumentHeuristicInfo> documents,
        IReadOnlyDictionary<string, int> inboundReferenceCounts)
    {
        // Annotations and folder conventions both contribute; annotations no longer suppress
        // every other signal. The inbound-reference quartile supplements among the rest.
        var primaryPathFileCount = documents.Count(document => document.HasPrimaryPathAnnotation || document.MatchesPrimaryPathConvention);
        var percentileCandidates = documents
            .Where(document => !document.HasPrimaryPathAnnotation && !document.MatchesPrimaryPathConvention)
            .Select(document => inboundReferenceCounts[document.FilePath])
            .ToArray();

        var percentileThreshold = GetTopQuartileThreshold(percentileCandidates);
        if (percentileThreshold is not null)
        {
            primaryPathFileCount += percentileCandidates.Count(count => count >= percentileThreshold.Value);
        }

        return primaryPathFileCount;
    }

    private static bool ShouldAnalyzeDocument(Document document)
    {
        return !string.IsNullOrWhiteSpace(document.FilePath) && SourceFileConventions.IsCountableSourceFile(document.FilePath);
    }

    private static async Task<DocumentHeuristicInfo> AnalyzeDocumentAsync(
        Document document,
        Dictionary<string, int> inboundReferenceCounts,
        CancellationToken cancellationToken)
    {
        var filePath = document.FilePath
            ?? throw new InvalidOperationException($"Document '{document.Name}' does not have a file path.");
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not load syntax for '{filePath}'.");
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not build a semantic model for '{filePath}'.");

        RecordOutboundReferences(syntaxRoot, semanticModel, filePath, inboundReferenceCounts, cancellationToken);

        return new DocumentHeuristicInfo(
            filePath,
            HasPrimaryPathAnnotation(syntaxRoot, semanticModel, cancellationToken),
            MatchesPrimaryPathConvention(filePath));
    }

    /// <summary>
    /// Walks the document once and attributes each resolved named-type reference to the
    /// analyzable documents that declare the type. Counting individual reference locations
    /// (not distinct referencing documents) and skipping same-file references preserves the
    /// semantics of the previous SymbolFinder-based implementation, which also reported
    /// implicit constructor locations — hence target-typed <c>new(...)</c> participates.
    /// </summary>
    private static void RecordOutboundReferences(
        SyntaxNode syntaxRoot,
        SemanticModel semanticModel,
        string referencingFilePath,
        Dictionary<string, int> inboundReferenceCounts,
        CancellationToken cancellationToken)
    {
        foreach (var node in syntaxRoot.DescendantNodes(descendIntoTrivia: true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsTypeReferenceCandidate(node))
            {
                continue;
            }

            var namedType = ResolveNamedType(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol);
            if (namedType is null)
            {
                continue;
            }

            RecordReferencedType(namedType, referencingFilePath, inboundReferenceCounts);
        }
    }

    private static bool IsTypeReferenceCandidate(SyntaxNode node)
    {
        return node switch
        {
            SimpleNameSyntax name => !name.IsVar,
            ImplicitObjectCreationExpressionSyntax => true,
            _ => false
        };
    }

    private static void RecordReferencedType(
        INamedTypeSymbol namedType,
        string referencingFilePath,
        Dictionary<string, int> inboundReferenceCounts)
    {
        var definitionFilePaths = namedType.OriginalDefinition.DeclaringSyntaxReferences
            .Select(reference => reference.SyntaxTree.FilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var definitionFilePath in definitionFilePaths)
        {
            if (!string.Equals(definitionFilePath, referencingFilePath, StringComparison.OrdinalIgnoreCase) &&
                inboundReferenceCounts.TryGetValue(definitionFilePath, out var count))
            {
                inboundReferenceCounts[definitionFilePath] = count + 1;
            }
        }
    }

    private static INamedTypeSymbol? ResolveNamedType(ISymbol? symbol)
    {
        return symbol switch
        {
            IAliasSymbol alias => ResolveNamedType(alias.Target),
            INamedTypeSymbol namedType => namedType,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType,
            _ => null
        };
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

    private static bool MatchesPrimaryPathConvention(string filePath)
    {
        return SourceFileConventions.ContainsDirectorySegment(filePath, "Controllers") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Endpoints") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Handlers") ||
               SourceFileConventions.ContainsDirectorySegment(filePath, "Pages");
    }

    internal static int? GetTopQuartileThreshold(IReadOnlyList<int> counts)
    {
        if (counts.Count == 0)
        {
            return null;
        }

        var sortedCounts = counts.OrderBy(count => count).ToArray();

        // A homogeneous distribution has no meaningful top quartile — without this guard every
        // file would qualify as primary path.
        if (sortedCounts[0] == sortedCounts[^1])
        {
            return null;
        }

        var thresholdIndex = (int)Math.Ceiling(sortedCounts.Length * 0.75d) - 1;
        thresholdIndex = Math.Clamp(thresholdIndex, 0, sortedCounts.Length - 1);

        var threshold = sortedCounts[thresholdIndex];
        return threshold > 0 ? threshold : null;
    }

    internal readonly record struct HeuristicMetrics(int PrimaryPathFileCount, int AnalyzedFileCount);

    private readonly record struct DocumentHeuristicInfo(
        string FilePath,
        bool HasPrimaryPathAnnotation,
        bool MatchesPrimaryPathConvention);
}
