namespace Sample.OverEngineered.Infrastructure.Serialization;

public interface ITextSerializer
{
    string Serialize<TValue>(TValue value);
}
