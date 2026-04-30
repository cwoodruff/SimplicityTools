namespace Sample.OverEngineered.Cache;

public sealed class NullCacheStore : ICacheStore
{
    private readonly Dictionary<string, string> _items = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, string value)
    {
        _items[key] = value;
    }

    public bool TryGet(string key, out string? value)
    {
        if (_items.TryGetValue(key, out var storedValue))
        {
            value = storedValue;
            return true;
        }

        value = null;
        return false;
    }
}
