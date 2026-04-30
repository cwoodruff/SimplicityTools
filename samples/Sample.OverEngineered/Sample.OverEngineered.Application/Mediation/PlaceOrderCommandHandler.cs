using Sample.OverEngineered.Cache;
using Sample.OverEngineered.Infrastructure.Serialization;
using Sample.OverEngineered.Messaging;
using Sample.OverEngineered.Persistence;
using Sample.OverEngineered.ReadModel;
using Sample.OverEngineered.Telemetry;
using Sample.OverEngineered.WriteModel;

namespace Sample.OverEngineered.Application;

public sealed class PlaceOrderCommandHandler : IPlaceOrderCommandHandler
{
    private readonly IOrderWriteFacade _orderWriteFacade;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderReadModelStore _orderReadModelStore;
    private readonly IOrderReadFacade _orderReadFacade;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheStore _cacheStore;
    private readonly ICacheKeyBuilder _cacheKeyBuilder;
    private readonly ITextSerializer _textSerializer;
    private readonly IPlaceOrderResultMapper _resultMapper;
    private readonly ITelemetrySink _telemetrySink;
    private readonly IActivityScopeFactory _activityScopeFactory;

    public PlaceOrderCommandHandler(
        IOrderWriteFacade orderWriteFacade,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IOrderReadModelStore orderReadModelStore,
        IOrderReadFacade orderReadFacade,
        IEventPublisher eventPublisher,
        ICacheStore cacheStore,
        ICacheKeyBuilder cacheKeyBuilder,
        ITextSerializer textSerializer,
        IPlaceOrderResultMapper resultMapper,
        ITelemetrySink telemetrySink,
        IActivityScopeFactory activityScopeFactory)
    {
        _orderWriteFacade = orderWriteFacade;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _orderReadModelStore = orderReadModelStore;
        _orderReadFacade = orderReadFacade;
        _eventPublisher = eventPublisher;
        _cacheStore = cacheStore;
        _cacheKeyBuilder = cacheKeyBuilder;
        _textSerializer = textSerializer;
        _resultMapper = resultMapper;
        _telemetrySink = telemetrySink;
        _activityScopeFactory = activityScopeFactory;
    }

    public PlaceOrderResult Handle(PlaceOrderCommand command)
    {
        using var scope = _activityScopeFactory.Create("application.place-order.handler");

        var order = _orderWriteFacade.CreateOrder(command.Input);
        _orderRepository.Save(order);
        _unitOfWork.Commit();

        var projection = _orderReadModelStore.Upsert(order);
        var cacheKey = _cacheKeyBuilder.Build(order.OrderNumber);
        _cacheStore.Set(cacheKey, _textSerializer.Serialize(projection));
        _eventPublisher.Publish("orders.placed", $"{order.OrderNumber}:{order.CustomerId}:{command.ActorId}");
        _telemetrySink.Track("command-handler.place-order.persisted", order.OrderNumber);

        var currentProjection = _orderReadFacade.Get(order.OrderNumber);
        return _resultMapper.Map(order, currentProjection);
    }
}
