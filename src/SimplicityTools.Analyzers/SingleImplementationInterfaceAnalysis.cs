using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SimplicityTools.Analyzers;

internal static class SingleImplementationInterfaceAnalysis
{
    public static INamedTypeSymbol? FindConcreteImplementation(
        Compilation compilation,
        INamedTypeSymbol interfaceSymbol,
        CancellationToken cancellationToken)
    {
        var interfaceDefinition = interfaceSymbol.OriginalDefinition;
        var implementations = SourceSymbolIndex.Create(compilation, cancellationToken)
            .NamedTypes
            .Where(static type => type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract)
            .Where(type => type.AllInterfaces.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, interfaceDefinition)))
            .Take(2)
            .ToImmutableArray();

        return implementations.Length == 1 ? implementations[0] : null;
    }
}
