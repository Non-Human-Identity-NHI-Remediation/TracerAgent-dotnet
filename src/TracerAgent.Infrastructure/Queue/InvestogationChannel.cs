namespace TracerAgent.Infrastructure.Queue;

using System.Threading.Channels;
using TracerAgent-dotnet.TracerAgent.Core.Models;

public sealed class InvestigationChannel
{
    private readonly Channel<InvestigationRequest> _channel;

    public InvestigationChannel()
    {
        _channel = Channel.CreateBounded<InvestigationRequest>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(InvestigationRequest request, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(request, ct);

    public IAsyncEnumerable<InvestigationRequest> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);

    public int PendingCount => _channel.Reader.Count;
}