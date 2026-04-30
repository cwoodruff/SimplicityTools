namespace Sample.Simplified.App.Payments;

public sealed record PaymentAuthorization(string ApprovalCode, bool Captured);
