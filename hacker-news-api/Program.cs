using System.Text.Json.Serialization;

namespace HackerNewsApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        bool useSseStream = true;
        var builder = WebApplication.CreateBuilder(args);

        //We could also have resilient clients as shown here https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        builder.Services.AddHttpClient<HackerNewsBestStoriesClient>();
        builder.Services.AddHttpClient<HackerNewsItemClient>();
        if (useSseStream)
        {
            builder.Services.AddSingleton<IHackerNewsStore, HackerNewsSseStore>();
            builder.Services.AddHostedService<HackerNewsSubscriptionService>();
        }
        else
        {
            builder.Services.AddSingleton<IHackerNewsStore, HackerNewsRestStore>();
        }

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.MapGet("/topstories", async (HttpContext httpContext, int n) =>
        {
            if (n <= 0 || n > 200)
            {
                throw new ArgumentException("n must be positive and <= 200");
            }
            var store = app.Services.GetRequiredService<IHackerNewsStore>();
            return (await store.GetStories(n))
                .Select(hns =>
                    new ResposeStory()
                    {
                        Title = hns.Title,
                        Uri = hns.Url ?? string.Empty,
                        PostedBy = hns.By,
                        Time = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(hns.Time),
                        Score = hns.Score,
                        CommentCount = hns.Descendants
                    })
                .ToArray();
        })
        .WithName("GetTopStories");

        app.Run();
    }
}

public class ResposeStory
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("uri")]
    public required string Uri { get; set; }

    [JsonPropertyName("postedBy")]
    public required string PostedBy { get; set; }

    [JsonPropertyName("time")]
    public required DateTime Time { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("commentCount")]
    public int CommentCount { get; set; }
}

public class HackerNewsStory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("by")]
    public required string By { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

public record HackerNewsItem<T>
{
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("data")]
    public required T Data { get; set; }
}