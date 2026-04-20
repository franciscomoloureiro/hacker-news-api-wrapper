using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace HackerNewsApi;

public class HackerNewsStream<T>
{
    private readonly Func<Task<Stream>> _streamFactory;
    private readonly BufferBlock<T> _bufferBlock;

    public HackerNewsStream(Func<Task<Stream>> streamFactory)
    {
        _streamFactory = streamFactory;
        _bufferBlock = new();
    }

    public Task Subscribe(Action<T> action, CancellationToken cancellationToken)
    {
        var actionBlock = new ActionBlock<T>(action, new ExecutionDataflowBlockOptions
        {
            CancellationToken = cancellationToken
        });

        _bufferBlock.LinkTo(actionBlock, new DataflowLinkOptions
        {
            PropagateCompletion = true
        });

        _ = Produce(cancellationToken);

        return _bufferBlock.Completion;
    }

    private async Task Produce(CancellationToken cancellationToken)
    {
        var stream = await _streamFactory();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (SseItem<HackerNewsItem<T>?> item in SseParser.Create(stream, (eventType, bytes) =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<HackerNewsItem<T>>(bytes);
                    }
                    catch (JsonException)
                    {
                        return default;
                    }
                }).EnumerateAsync(cancellationToken))
                {
                    if (item.Data is null)
                    {
                        continue;
                    }
                    _bufferBlock.Post(item.Data.Data);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                //we would need better handling
            }
            //We got disconnected for some reason we should connect again
            //smarter strategies for this need to be implemented for robust system
            //first thing would be understanding if we should recconect in the first place, or backoff from fb as it migh be down or overloaded
            stream.Dispose();
            stream = await _streamFactory();
        }
        Console.WriteLine("Stream end");
        stream.Dispose();
        _bufferBlock.Complete();
    }
}
