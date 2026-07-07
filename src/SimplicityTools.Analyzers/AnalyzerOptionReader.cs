using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SimplicityTools.Analyzers;

/// <summary>
/// Reads analyzer configuration knobs from analyzer config options (.editorconfig or
/// .globalconfig). Invalid or missing values fall back to the built-in defaults silently.
/// </summary>
internal static class AnalyzerOptionReader
{
    public const string ComplexityThresholdKey = "simplicity_first.sf0003_complexity_threshold";
    public const string LayerThresholdKey = "simplicity_first.sf0004_layer_threshold";
    public const string ParameterThresholdKey = "simplicity_first.sf0005_parameter_threshold";
    public const string ExcludedPackagesKey = "simplicity_first.sf0002_excluded_packages";
    public const string ConventionFoldersKey = "simplicity_first.sf0007_convention_folders";
    public const string IncludePublicApiKey = "simplicity_first.include_public_api";

    public static int GetThreshold(AnalyzerOptions options, SyntaxTree? tree, string key, int defaultValue)
    {
        if (TryGetValue(options, tree, key, out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return defaultValue;
    }

    public static bool GetFlag(AnalyzerOptions options, SyntaxTree? tree, string key, bool defaultValue)
    {
        if (TryGetValue(options, tree, key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    public static ImmutableHashSet<string> GetNameSet(AnalyzerOptions options, SyntaxTree? tree, string key)
    {
        if (!TryGetValue(options, tree, key, out var raw))
        {
            return ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
        }

        return SplitNames(raw).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static ImmutableArray<string> GetNameList(AnalyzerOptions options, SyntaxTree? tree, string key, ImmutableArray<string> defaultValue)
    {
        if (!TryGetValue(options, tree, key, out var raw))
        {
            return defaultValue;
        }

        var names = SplitNames(raw).ToImmutableArray();
        return names.IsEmpty ? defaultValue : names;
    }

    public static SyntaxTree? GetDeclaringTree(ISymbol symbol)
        => symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;

    private static IEnumerable<string> SplitNames(string raw)
        => raw.Split(',')
            .Select(static name => name.Trim())
            .Where(static name => name.Length > 0);

    private static bool TryGetValue(AnalyzerOptions options, SyntaxTree? tree, string key, out string value)
    {
        var provider = options.AnalyzerConfigOptionsProvider;
        if (tree is not null &&
            provider.GetOptions(tree).TryGetValue(key, out value!) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return provider.GlobalOptions.TryGetValue(key, out value!) && !string.IsNullOrWhiteSpace(value);
    }
}
