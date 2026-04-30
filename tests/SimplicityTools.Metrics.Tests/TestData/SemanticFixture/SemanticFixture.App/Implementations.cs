using Humanizer;

namespace SemanticFixture.App;

public sealed class HumanizedReportFormatter(bool enabled) : IReportFormatter
{
    private readonly string? label = enabled ? "enabled" : null;

    public HumanizedReportFormatter()
        : this(enabled: false)
    {
    }

    public string Label
    {
        get
        {
            return label ?? "pending";
        }
    }

    public string Format(int count)
    {
        return count.ToWords();
    }

    public int Score(bool left, bool right)
    {
        return left && right ? 1 : 0;
    }
}

public sealed class EnabledModeSelector : IModeSelector
{
    public string Select(bool enabled)
    {
        return enabled ? "on" : "off";
    }
}

public sealed class DisabledModeSelector : IModeSelector
{
    public string Select(bool enabled)
    {
        return enabled ? "skip" : "hold";
    }
}
