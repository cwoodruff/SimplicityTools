namespace Sample.OverEngineered.Persistence;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }

    public void Commit()
    {
        CommitCount++;
    }
}
