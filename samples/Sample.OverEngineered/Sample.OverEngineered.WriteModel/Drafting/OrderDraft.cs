using Sample.OverEngineered.Domain.Entities;

namespace Sample.OverEngineered.WriteModel;

public sealed record OrderDraft(
    string CustomerId,
    IReadOnlyList<OrderLine> Lines,
    string Notes,
    DateTimeOffset CapturedAt,
    string CorrelationId);
