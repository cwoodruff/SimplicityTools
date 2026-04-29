namespace Sample.OverEngineered.Domain.Entities;

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}
