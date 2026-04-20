using System.Collections.Concurrent;

namespace HackerNewsApi;

public interface IHackerNewsStore
{
    Task<IReadOnlyList<HackerNewsStory>> GetStories(int n);

}
public class HackerNewsSseStore : IHackerNewsStore, IDisposable
{
    private readonly HackerNewsBestStoriesClient _bestStoriesClient;
    private readonly HackerNewsItemClient _itemClient;
    private readonly ConcurrentDictionary<int, (HackerNewsStory, CancellationTokenSource)> _stories = new();

    private HackerNewsStream<int[]>? _bestStoriesStream;

    public HackerNewsSseStore(HackerNewsBestStoriesClient bestStoriesClient, HackerNewsItemClient itemClient)
    {
        _bestStoriesClient = bestStoriesClient;
        _itemClient = itemClient;
    }

    public Task<IReadOnlyList<HackerNewsStory>> GetStories(int n)
    {
        return Task.FromResult<IReadOnlyList<HackerNewsStory>>(_stories
                .Values
                .Select(t => t.Item1)
                .OrderByDescending(hns => hns.score)
                .ThenByDescending(hns => hns.id)
                .Take(n)
                .ToList()
                );
    }

    public Task StartStore(CancellationToken cancellationToken)
    {
        if (_bestStoriesStream != null)
        {
            //Stream already started, we return
            return Task.CompletedTask;
        }

        _bestStoriesStream = _bestStoriesClient.GetBestStoriesStream();
        return _bestStoriesStream.Subscribe((stories) =>
        {
            Task.WhenAll(stories.Select(SubscribeToStory));
            //Dispose old stories
            foreach (var kvp in _stories)
            {
                if (!stories.Contains(kvp.Key))
                {
                    kvp.Value.Item2.Cancel();
                }
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        foreach (var token in _stories.Values.Select(t => t.Item2))
        {
            token.Cancel();
        }
    }

    private Task SubscribeToStory(int storyId)
    {
        if (!_stories.ContainsKey(storyId))
        {
            var cts = new CancellationTokenSource();
            var stream = _itemClient.GetItemStream(storyId);
            cts.Token.Register(() =>
            {
                //We hold a reference so we can remove
                _stories.TryRemove(storyId, out _);
            });
            return stream.Subscribe(
                hns => _stories.AddOrUpdate(storyId, (hns, cts), (i, t) => (hns, cts)),
                cts.Token
            );
        }
        return Task.CompletedTask;
    }
}

public class HackerNewsRestStore : IHackerNewsStore
{
    private readonly TimeSpan _bestStoriesTTL = TimeSpan.FromSeconds(30);
    private readonly HackerNewsBestStoriesClient _bestStoriesClient;
    private readonly HackerNewsItemClient _itemClient;
    private readonly ConcurrentDictionary<int, HackerNewsStory> _stories = new();
    private readonly Lock _lock = new();
    private Task? _refreshTask = null;
    private DateTime _lastFetchTime = DateTime.MinValue;

    public HackerNewsRestStore(HackerNewsBestStoriesClient bestStoriesClient, HackerNewsItemClient itemClient)
    {
        _bestStoriesClient = bestStoriesClient;
        _itemClient = itemClient;
    }

    public async Task<IReadOnlyList<HackerNewsStory>> GetStories(int n)
    {
        if ((DateTime.Now - _lastFetchTime) > _bestStoriesTTL)
        {
            //time to live pass, We refresh data
            lock (_lock)
            {
                _refreshTask ??= RefreshData();
            }
            await _refreshTask;
            _refreshTask = null;
        }
        return _stories
                .Values
                .OrderByDescending(hns => hns.score)
                .ThenByDescending(hns => hns.id)
                .Take(n)
                .ToList();
    }

    private async Task RefreshData()
    {
        _stories.Clear();

        var cts = new CancellationTokenSource();
        var bestStoriesIds = await _bestStoriesClient.GetBestStoriesAsync(cts.Token);
        var stories = await Task.WhenAll(bestStoriesIds.Select(GetStory));

        foreach (var story in stories)
        {
            _stories.TryAdd(story.id, story);
        }

        _lastFetchTime = DateTime.Now;
    }

    private Task<HackerNewsStory> GetStory(int id)
        => _itemClient.GetItemAsync(id, new CancellationTokenSource().Token);
}

