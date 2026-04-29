using System.Threading.Channels;

namespace FreakLete.Api.Services.Embeddings;

public sealed class EmbeddingChannel
{
    private readonly Channel<EmbeddingJob> _channel = Channel.CreateBounded<EmbeddingJob>(
        new BoundedChannelOptions(capacity: 256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<EmbeddingJob> Reader => _channel.Reader;

    public bool TryWrite(EmbeddingJob job) => _channel.Writer.TryWrite(job);
}
