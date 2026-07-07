using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SimplicityTools.Analyzers;

public static class PackageReferenceAnalysis
{
    public const string PackageIdPropertyName = "PackageId";

    public static ImmutableArray<PackageReferenceInfo> CollectPackageReferences(AnalyzerOptions options)
    {
        if (TryGetProjectFileText(options, out var projectPath, out var sourceText))
        {
            return ParsePackageReferences(projectPath, sourceText);
        }

        return ImmutableArray<PackageReferenceInfo>.Empty;
    }

    public static ImmutableDictionary<string, ImmutableArray<IAssemblySymbol>> MapPackagesToAssemblies(Compilation compilation)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<IAssemblySymbol>>();
        var buckets = new Dictionary<string, List<IAssemblySymbol>>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
        {
            var filePath = reference.FilePath;
            if (filePath is null ||
                string.IsNullOrWhiteSpace(filePath) ||
                !TryGetPackageIdFromPath(filePath, out var packageId) ||
                compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            if (!buckets.TryGetValue(packageId, out var assemblies))
            {
                assemblies = [];
                buckets[packageId] = assemblies;
            }

            assemblies.Add(assembly);
        }

        foreach (var pair in buckets)
        {
            builder[pair.Key] = ImmutableArray.CreateRange(new HashSet<IAssemblySymbol>(pair.Value, SymbolEqualityComparer.Default));
        }

        return builder.ToImmutable();
    }

    public static ImmutableDictionary<string, ImmutableArray<string>> BuildPackageIdsByAssemblyIdentity(
        ImmutableDictionary<string, ImmutableArray<IAssemblySymbol>> assembliesByPackage)
    {
        var packageIdsByAssemblyIdentity = new Dictionary<string, ImmutableArray<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in assembliesByPackage)
        {
            foreach (var assembly in pair.Value)
            {
                var key = assembly.Identity.ToString();
                if (!packageIdsByAssemblyIdentity.TryGetValue(key, out var packageIds))
                {
                    packageIdsByAssemblyIdentity[key] = ImmutableArray.Create(pair.Key);
                    continue;
                }

                packageIdsByAssemblyIdentity[key] = packageIds.Add(pair.Key);
            }
        }

        return packageIdsByAssemblyIdentity.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public static void CollectUsedPackages(
        SemanticModel semanticModel,
        ImmutableDictionary<string, ImmutableArray<string>> packageIdsByAssemblyIdentity,
        Action<string> onUsedPackage,
        CancellationToken cancellationToken)
    {
        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var assembly in CollectAssemblies(node, semanticModel, cancellationToken))
            {
                if (packageIdsByAssemblyIdentity.TryGetValue(assembly.Identity.ToString(), out var packageIds))
                {
                    foreach (var packageId in packageIds)
                    {
                        onUsedPackage(packageId);
                    }
                }
            }
        }
    }

    public static bool TryRemovePackageReference(SourceText sourceText, string packageId, TextSpan diagnosticSpan, out SourceText updatedSourceText)
    {
        var packageReference = TryFindPackageReference(sourceText, packageId, diagnosticSpan);
        if (packageReference is null)
        {
            updatedSourceText = sourceText;
            return false;
        }

        var span = GetRemovalSpan(sourceText, packageReference);
        if (span.Length == 0)
        {
            updatedSourceText = sourceText;
            return false;
        }

        updatedSourceText = sourceText.WithChanges(new TextChange(span, string.Empty));
        try
        {
            _ = XDocument.Parse(updatedSourceText.ToString(), LoadOptions.PreserveWhitespace);
            return true;
        }
        catch (XmlException)
        {
            updatedSourceText = sourceText;
            return false;
        }
    }

    private static bool TryGetProjectFileText(AnalyzerOptions options, out string projectPath, out SourceText sourceText)
    {
        // The project file must be provided as an AdditionalFile (wired up by the packaged
        // build/buildTransitive props). Analyzers must never perform file I/O themselves.
        var projectFile = options.AdditionalFiles.FirstOrDefault(file =>
            string.Equals(Path.GetExtension(file.Path), ".csproj", StringComparison.OrdinalIgnoreCase));
        if (projectFile is not null)
        {
            projectPath = projectFile.Path;
            sourceText = projectFile.GetText(CancellationToken.None) ?? SourceText.From(string.Empty);
            return true;
        }

        projectPath = string.Empty;
        sourceText = SourceText.From(string.Empty);
        return false;
    }

    private static ImmutableArray<PackageReferenceInfo> ParsePackageReferences(string projectPath, SourceText sourceText)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(sourceText.ToString(), LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return ImmutableArray<PackageReferenceInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<PackageReferenceInfo>();

        foreach (var element in document.Descendants().Where(static element => element.Name.LocalName == "PackageReference"))
        {
            var include = element.Attribute("Include")?.Value;
            if (include is null || string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var lineInfo = (IXmlLineInfo)element;
            var lineNumber = Math.Max(1, lineInfo.LineNumber);
            var line = sourceText.Lines[Math.Min(lineNumber - 1, sourceText.Lines.Count - 1)];
            var location = Location.Create(
                projectPath,
                line.Span,
                new LinePositionSpan(new LinePosition(line.LineNumber, 0), new LinePosition(line.LineNumber, line.Span.Length)));

            builder.Add(new PackageReferenceInfo(include, NormalizePackageId(include), location));
        }

        return builder.ToImmutable();
    }

    private static XElement? TryFindPackageReference(SourceText sourceText, string packageId, TextSpan diagnosticSpan)
    {
        var document = XDocument.Parse(sourceText.ToString(), LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var diagnosticLine = sourceText.Lines.GetLineFromPosition(diagnosticSpan.Start).LineNumber;

        return document.Descendants()
            .Where(static element => element.Name.LocalName == "PackageReference")
            .FirstOrDefault(element =>
                string.Equals(element.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase) &&
                IsMatchingDiagnosticLine(sourceText, element, diagnosticLine))
            ?? document.Descendants()
                .Where(static element => element.Name.LocalName == "PackageReference")
                .FirstOrDefault(element =>
                    string.Equals(element.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMatchingDiagnosticLine(SourceText sourceText, XElement element, int diagnosticLine)
    {
        var lineInfo = (IXmlLineInfo)element;
        if (!lineInfo.HasLineInfo())
        {
            return false;
        }

        return sourceText.Lines[Math.Max(0, lineInfo.LineNumber - 1)].LineNumber == diagnosticLine;
    }

    private static TextSpan GetRemovalSpan(SourceText sourceText, XElement element)
    {
        var lineInfo = (IXmlLineInfo)element;
        if (!lineInfo.HasLineInfo())
        {
            return default;
        }

        var startLineIndex = Math.Max(0, lineInfo.LineNumber - 1);
        if (startLineIndex >= sourceText.Lines.Count)
        {
            return default;
        }

        var start = sourceText.Lines[startLineIndex].Start + Math.Max(0, lineInfo.LinePosition - 1);
        var end = FindElementEnd(sourceText.ToString(), start);
        if (end <= start)
        {
            return default;
        }

        var startLine = sourceText.Lines[startLineIndex];
        var endLine = sourceText.Lines.GetLineFromPosition(Math.Max(start, end - 1));
        return TextSpan.FromBounds(startLine.Start, endLine.EndIncludingLineBreak);
    }

    private static int FindElementEnd(string text, int start)
    {
        var startTagEnd = text.IndexOf('>', start);
        if (startTagEnd < 0)
        {
            return -1;
        }

        if (startTagEnd > start && text[startTagEnd - 1] == '/')
        {
            return startTagEnd + 1;
        }

        const string closingTag = "</PackageReference>";
        var closingTagStart = text.IndexOf(closingTag, startTagEnd + 1, StringComparison.OrdinalIgnoreCase);
        return closingTagStart < 0 ? -1 : closingTagStart + closingTag.Length;
    }

    private static IEnumerable<IAssemblySymbol> CollectAssemblies(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var seen = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
        AddSymbolAssemblies(symbolInfo.Symbol, seen);
        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            AddSymbolAssemblies(candidate, seen);
        }

        var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
        AddTypeAssembly(typeInfo.Type, seen);
        AddTypeAssembly(typeInfo.ConvertedType, seen);

        return seen;
    }

    private static void AddSymbolAssemblies(ISymbol? symbol, ISet<IAssemblySymbol> assemblies)
    {
        switch (symbol)
        {
            case null:
                return;
            case IAliasSymbol aliasSymbol:
                AddSymbolAssemblies(aliasSymbol.Target, assemblies);
                return;
        }

        if (symbol.ContainingAssembly is not null)
        {
            assemblies.Add(symbol.ContainingAssembly);
        }

        switch (symbol)
        {
            case INamedTypeSymbol namedType:
                AddTypeAssembly(namedType, assemblies);
                break;
            case IMethodSymbol method:
                AddTypeAssembly(method.ReturnType, assemblies);
                foreach (var parameter in method.Parameters)
                {
                    AddTypeAssembly(parameter.Type, assemblies);
                }

                break;
            case IPropertySymbol property:
                AddTypeAssembly(property.Type, assemblies);
                break;
            case IFieldSymbol field:
                AddTypeAssembly(field.Type, assemblies);
                break;
            case ILocalSymbol local:
                AddTypeAssembly(local.Type, assemblies);
                break;
            case IParameterSymbol parameter:
                AddTypeAssembly(parameter.Type, assemblies);
                break;
        }
    }

    private static void AddTypeAssembly(ITypeSymbol? typeSymbol, ISet<IAssemblySymbol> assemblies)
    {
        switch (typeSymbol)
        {
            case null:
                return;
            case IArrayTypeSymbol arrayType:
                AddTypeAssembly(arrayType.ElementType, assemblies);
                return;
            case IPointerTypeSymbol pointerType:
                AddTypeAssembly(pointerType.PointedAtType, assemblies);
                return;
        }

        if (typeSymbol.ContainingAssembly is not null)
        {
            assemblies.Add(typeSymbol.ContainingAssembly);
        }

        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return;
        }

        foreach (var typeArgument in namedType.TypeArguments)
        {
            AddTypeAssembly(typeArgument, assemblies);
        }
    }

    private static bool TryGetPackageIdFromPath(string filePath, out string packageId)
    {
        var normalized = filePath.Replace('\\', '/');
        const string marker = "/packages/";
        var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            packageId = string.Empty;
            return false;
        }

        var packageStart = markerIndex + marker.Length;
        var packageEnd = normalized.IndexOf('/', packageStart);
        if (packageEnd < 0)
        {
            packageId = string.Empty;
            return false;
        }

        packageId = normalized.Substring(packageStart, packageEnd - packageStart);
        return !string.IsNullOrWhiteSpace(packageId);
    }

    private static string NormalizePackageId(string packageId)
        => packageId.Trim().ToLowerInvariant();
}

public sealed record PackageReferenceInfo(string PackageId, string NormalizedPackageId, Location Location);
