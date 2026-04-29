using Sample.OverEngineered.Domain.Contracts;

namespace Sample.OverEngineered.Application;

public sealed record PlaceOrderCommand(PlaceOrderInput Input, string ActorId) : ICommand<PlaceOrderResult>;
