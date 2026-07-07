using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimplicityTools.Analyzers;

internal static class PrimaryPathConventions
{
    private static readonly string[] ConventionalSegments = ["Controllers", "Endpoints", "Handlers", "Pages"];

    public static bool IsPrimaryPathAnnotated(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var constructor = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol as IMethodSymbol;
            if (constructor?.ContainingType.ToDisplayString() == "SimplicityTools.Metrics.PrimaryPathAttribute")
            {
                return true;
            }
        }

        return false;
    }

    public static bool MatchesPrimaryPathConvention(string? filePath)
    {
        return filePath is not null &&
               !string.IsNullOrWhiteSpace(filePath) &&
               ConventionalSegments.Any(segment => AnalyzerSourceFileConventions.ContainsDirectorySegment(filePath, segment));
    }
}
