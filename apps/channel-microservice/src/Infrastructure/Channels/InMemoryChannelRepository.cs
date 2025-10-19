using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Domain.Channels;

namespace ChannelMicroservice.Infrastructure.Channels;

public sealed class InMemoryChannelRepository : IChannelRepository
{
    private readonly ConcurrentDictionary<string, Channel> _byId = new();
    private readonly ConcurrentDictionary<string, string> _nameIndex = new();

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => Task.FromResult(_nameIndex.ContainsKey(name));

    public Task AddAsync(Channel channel, CancellationToken ct)
    {
        _byId[channel.Id] = channel;
        _nameIndex[channel.Name] = channel.Id;
        return Task.CompletedTask;
    }
}