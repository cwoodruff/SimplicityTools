namespace Sample.OverEngineered.ReadModel;

public interface IOrderReadFacade
{
    OrderReadModel? Get(string orderNumber);
}
