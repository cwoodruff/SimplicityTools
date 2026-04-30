namespace Sample.OverEngineered.Cache;

public interface ICacheKeyBuilder
{
    string Build(string orderNumber);
}
