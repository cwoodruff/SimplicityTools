using System.Collections.Generic;

namespace Sample.Simplified.App.Catalog;

public sealed class ProductCatalog
{
    private readonly IReadOnlyDictionary<string, Product> products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
    {
        ["BEANS-COLOMBIA"] = new("BEANS-COLOMBIA", "Colombia Roast", 18.50m),
        ["FILTERS-PAPER"] = new("FILTERS-PAPER", "Paper Filters", 8.75m),
        ["MUG-CAMP"] = new("MUG-CAMP", "Camp Mug", 16.25m)
    };

    public Product GetBySku(string sku)
    {
        if (products.TryGetValue(sku, out var product))
        {
            return product;
        }

        throw new InvalidOperationException($"Unknown product '{sku}'.");
    }
}
