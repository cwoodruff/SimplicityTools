namespace Sample.Simplified.App.Orders;

public sealed record OrderLine(string Sku, string ProductName, int Quantity, decimal UnitPrice)
{
    public decimal Subtotal => Quantity * UnitPrice;
}
