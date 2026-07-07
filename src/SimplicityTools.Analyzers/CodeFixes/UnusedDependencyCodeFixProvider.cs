using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace SimplicityTools.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedDependencyCodeFixProvider))]
[Shared]
public sealed class UnusedDependencyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [UnusedDependencyAnalyzer.DiagnosticId];

    // The batch fixer merges text changes against the original csproj text, so overlapping
    // multi-package removals effectively apply one package at a time. This is documented
    // behavior (docs/using-the-simplicity-tools.md); each removal remains a single click.
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var textDocument = context.TextDocument;
        var diagnostic = context.Diagnostics[0];
        if (!diagnostic.Properties.TryGetValue(PackageReferenceAnalysis.PackageIdPropertyName, out var packageId) ||
            packageId is null ||
            string.IsNullOrWhiteSpace(packageId))
        {
            return;
        }

        var sourceText = await textDocument.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
        if (!PackageReferenceAnalysis.TryRemovePackageReference(sourceText, packageId, diagnostic.Location.SourceSpan, out var updatedText))
        {
            return;
        }

        var title = $"Remove PackageReference '{packageId}'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => Task.FromResult(ApplyTextChange(textDocument, updatedText)),
                equivalenceKey: title),
            diagnostic);
    }

    private static Solution ApplyTextChange(TextDocument textDocument, SourceText updatedText)
    {
        var solution = textDocument.Project.Solution;
        return textDocument switch
        {
            Document document => solution.WithDocumentText(document.Id, updatedText),
            _ => solution.WithAdditionalDocumentText(textDocument.Id, updatedText)
        };
    }
}
