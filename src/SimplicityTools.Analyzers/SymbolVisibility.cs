using Microsoft.CodeAnalysis;

namespace SimplicityTools.Analyzers;

public static class SymbolVisibility
{
    /// <summary>
    /// Returns true when the symbol's effective accessibility makes it visible outside its
    /// assembly (public, or protected on a chain of externally visible containing types).
    /// </summary>
    public static bool IsExternallyVisible(ISymbol symbol)
    {
        for (var current = symbol; current is not null && current.Kind != SymbolKind.Namespace; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }
}
