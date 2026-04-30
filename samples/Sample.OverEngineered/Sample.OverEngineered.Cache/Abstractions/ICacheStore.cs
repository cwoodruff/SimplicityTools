namespace Sample.OverEngineered.Cache;

public interface ICacheStore
{
    void Set(string key, string value);

    bool TryGet(string key, out string? value);
}
