namespace SemanticFixture.App;

public interface IReportFormatter
{
    string Format(int count);
}

public interface IModeSelector
{
    string Select(bool enabled);
}
