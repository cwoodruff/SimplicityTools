using Sample.OverEngineered.Domain.ValueObjects;

namespace Sample.OverEngineered.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderLine> _lines;

    public Order(string orderNumber, string customerId, IEnumerable<OrderLine> lines, string notes, DateTimeOffset createdAt)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Notes = notes;
        CreatedAt = createdAt;
        _lines = lines.ToList();
    }

    public string OrderNumber { get; }

    public string CustomerId { get; }

    public string Notes { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Money Total => new(_lines.Sum(line => line.LineTotal), "USD");
}
