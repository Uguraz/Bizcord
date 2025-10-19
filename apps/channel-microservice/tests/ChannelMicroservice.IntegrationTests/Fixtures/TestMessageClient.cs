using System.Collections.Concurrent;
using ChannelMicroservice.Messaging;

namespace ChannelMicroservice.IntegrationTests.Fixtures;

public sealed class TestMessageClient : IMessageClient
{
    public sealed record Published(object Event, string? Topic);
    private readonly ConcurrentBag<Published> _published = new();
    public IEnumerable<Published> PublishedEvents => _published;

    public Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default)
    {
        _published.Add(new Published(message!, topic));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}