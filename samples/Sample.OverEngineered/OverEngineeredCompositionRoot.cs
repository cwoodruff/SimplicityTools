using Sample.OverEngineered.Application;
using Sample.OverEngineered.Authorization;
using Sample.OverEngineered.Cache;
using Sample.OverEngineered.Domain.Services;
using Sample.OverEngineered.Infrastructure.Correlation;
using Sample.OverEngineered.Infrastructure.Serialization;
using Sample.OverEngineered.Infrastructure.Time;
using Sample.OverEngineered.Messaging;
using Sample.OverEngineered.Persistence;
using Sample.OverEngineered.Presentation;
using Sample.OverEngineered.ReadModel;
using Sample.OverEngineered.Telemetry;
using Sample.OverEngineered.Validation;
using Sample.OverEngineered.WriteModel;

namespace Sample.OverEngineered;

internal sealed class OverEngineeredCompositionRoot
{
    private readonly IPlaceOrderApplicationService _applicationService;

    private OverEngineeredCompositionRoot(IPlaceOrderApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    public static OverEngineeredCompositionRoot Create()
    {
        ISystemClock clock = new SystemClock();
        ICorrelationIdAccessor correlationIdAccessor = new StaticCorrelationIdAccessor("corr-overengineered-001");
        ITextSerializer serializer = new JsonTextSerializer();
        IOrderNumberGenerator orderNumberGenerator = new SequentialOrderNumberGenerator();

        IRoleResolver roleResolver = new RoleResolver();
        IAuthorizationService authorizationService = new AuthorizationService(roleResolver);
        ICacheStore cacheStore = new NullCacheStore();
        ICacheKeyBuilder cacheKeyBuilder = new CacheKeyBuilder(correlationIdAccessor);
        IOrderDraftFactory orderDraftFactory = new OrderDraftFactory(clock, correlationIdAccessor);
        IOrderWriteFacade orderWriteFacade = new OrderWriteFacade(orderDraftFactory, orderNumberGenerator);
        IOrderRepository orderRepository = new SqlOrderRepository(clock, serializer);
        IUnitOfWork unitOfWork = new InMemoryUnitOfWork();
        IOrderReadModelStore orderReadModelStore = new OrderReadModelStore(cacheStore, cacheKeyBuilder);
        IOrderReadFacade orderReadFacade = new OrderReadFacade(orderReadModelStore);
        IEnvelopeFactory envelopeFactory = new EnvelopeFactory(clock, correlationIdAccessor);
        IEventPublisher eventPublisher = new EventPublisher(envelopeFactory);
        ITelemetrySink telemetrySink = new ConsoleTelemetrySink();
        IActivityScopeFactory activityScopeFactory = new ActivityScopeFactory(clock, telemetrySink);
        IValidationRule[] rules = [new RequireCustomerIdRule(), new RequireLineItemsRule()];
        IOrderValidator orderValidator = new OrderValidator(rules);
        IPlaceOrderResultMapper resultMapper = new PlaceOrderResultMapper();
        IPlaceOrderCommandHandler handler = new PlaceOrderCommandHandler(
            orderWriteFacade,
            orderRepository,
            unitOfWork,
            orderReadModelStore,
            orderReadFacade,
            eventPublisher,
            cacheStore,
            cacheKeyBuilder,
            serializer,
            resultMapper,
            telemetrySink,
            activityScopeFactory);
        ICommandBus commandBus = new CommandBus(handler);
        IPlaceOrderApplicationService applicationService = new PlaceOrderApplicationService(
            authorizationService,
            orderValidator,
            commandBus,
            telemetrySink);

        return new OverEngineeredCompositionRoot(applicationService);
    }

    public OrderEndpoint CreateOrderEndpoint() => new(_applicationService);
}
