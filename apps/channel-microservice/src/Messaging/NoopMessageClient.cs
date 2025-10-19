using System.Threading;
using System.Threading.Tasks;

namespace ChannelMicroservice.Messaging;

public sealed class NoopMessageClient : IMessageClient
{
    public Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}