namespace HackerNewsApi;

public class HackerNewsSubscriptionService : IHostedService
{
    private readonly HackerNewsSseStore _store;

    public HackerNewsSubscriptionService(IHackerNewsStore store)
    {
        _store = (HackerNewsSseStore)store;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _store.StartStore(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _store.Dispose();
        return Task.CompletedTask;
    }
}
