using System.Text.Json;

namespace HackerNewsApi;

public abstract class HackerNewsClient
{
    private readonly HttpClient _client;

    public HackerNewsClient(HttpClient client)
    {
        _client = client;
    }

    protected async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/event-stream");

        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    protected async Task<HttpResponseMessage> GetRequestAsync(string url, CancellationToken cancellationToken)
    {
        return await _client.GetAsync(url, cancellationToken);
    }
}

public class HackerNewsItemClient(HttpClient httpClient) : HackerNewsClient(httpClient)
{
    public HackerNewsStream<HackerNewsStory> GetItemStream(int id)
        => new(() => GetStreamAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json"));

    public async Task<HackerNewsStory> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var request = await GetRequestAsync($"https://hacker-news.firebaseio.com/v0/item/{id}.json", cancellationToken);
        return JsonSerializer.Deserialize<HackerNewsStory>(await request.Content.ReadAsStringAsync());
    }
}

public class HackerNewsBestStoriesClient(HttpClient client) : HackerNewsClient(client)
{
    public HackerNewsStream<int[]> GetBestStoriesStream()
        => new (() => GetStreamAsync($"https://hacker-news.firebaseio.com/v0/beststories.json"));

    public async Task<int[]> GetBestStoriesAsync(CancellationToken cancellationToken)
    {
        var request = await GetRequestAsync($"https://hacker-news.firebaseio.com/v0/beststories.json", cancellationToken);
        return JsonSerializer.Deserialize<int[]>(await request.Content.ReadAsStringAsync());
    }
}
