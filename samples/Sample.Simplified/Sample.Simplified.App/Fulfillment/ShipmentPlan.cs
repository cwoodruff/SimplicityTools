namespace Sample.Simplified.App.Fulfillment;

public sealed record ShipmentPlan(string Lane, int EstimatedDays, bool RequiresSignature);
