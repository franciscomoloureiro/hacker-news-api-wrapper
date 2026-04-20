using System.Text.Json;

namespace HackerNewsApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        bool useSseStream = true;
        var builder = WebApplication.CreateBuilder(args);

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
            return (await store.GetStories(n)).Select(hns => hns.id).ToArray();
        })
        .WithName("GetTopStories");

        app.MapGet("/story", async (HttpContext httpContext, int n) =>
        {
            var store = app.Services.GetRequiredService<IHackerNewsStore>();
            return (await store.GetStories(200)).Where(s => s.id == n).ToArray();
        })
      .WithName("Story");

        app.Run();
    }
}

public record HackerNewsComment(string by, int id, int[] kids, int parent, string text, long time, string type);
public record HackerNewsStory(int id, string title, string text, string by, long time, int score, int[] kids, int descendants, string type);
public record HackerNewsItem<T>(string path, T data);