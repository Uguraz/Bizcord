using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelMicroservice.Messaging;

public interface IMessageClient : IAsyncDisposable
{
    Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default);
}