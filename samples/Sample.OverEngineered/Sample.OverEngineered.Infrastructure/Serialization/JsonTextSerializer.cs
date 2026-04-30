using System.Text.Json;

namespace Sample.OverEngineered.Infrastructure.Serialization;

public sealed class JsonTextSerializer : ITextSerializer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, Options);
}
