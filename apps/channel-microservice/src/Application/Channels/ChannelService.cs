using System;
using System.Threading;
using System.Threading.Tasks;
using ChannelMicroservice.Contracts.Channels;
using ChannelMicroservice.Contracts.Channels.Events;
using ChannelMicroservice.Domain.Channels;
using ChannelMicroservice.Messaging;

namespace ChannelMicroservice.Application.Channels;

public sealed class ChannelService
{
    private readonly IChannelRepository _repo;
    private readonly IMessageClient _bus;

    public ChannelService(IChannelRepository repo, IMessageClient bus)
    {
        _repo = repo;
        _bus = bus;
    }

    public async Task<ChannelDto> CreateAsync(CreateChannelRequest req, CancellationToken ct)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name is required", nameof(req));

        var name = req.Name.Trim();
        if (name.Length > 100) throw new ArgumentException("Name too long (max 100)");

        if (await _repo.ExistsByNameAsync(name, ct))
            throw new InvalidOperationException("Channel name must be unique");

        var channel = new Channel(name);
        await _repo.AddAsync(channel, ct);

        var evt = new ChannelCreated(channel.Id, channel.Name, channel.CreatedAt);
        await _bus.PublishAsync(evt, topic: "channel.created", ct);

        return new ChannelDto(channel.Id, channel.Name, channel.CreatedAt);
    }
}