using ChannelMicroservice.Contracts.Channels;
using ChannelMicroservice.Contracts.Channels.Events;
using ChannelMicroservice.Domain.Channels;
using ChannelMicroservice.Messaging;
using Polly;
using Polly.Retry;

namespace ChannelMicroservice.Application.Channels;

public class ChannelService
{
    private readonly IChannelRepository _repo;
    private readonly IMessageClient _bus;
    private readonly AsyncRetryPolicy _publishRetryPolicy;

    public ChannelService(IChannelRepository repo, IMessageClient bus)
    {
        _repo = repo;
        _bus = bus;

        // 🔁 POLLY RETRY POLICY – håndterer fejl ved event publishing
        _publishRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(200 * attempt), // 200ms, 400ms, 600ms
                onRetry: (exception, delay, attempt, _) =>
                {
                    Console.WriteLine(
                        $"[Retry {attempt}] Failed to publish ChannelCreated. " +
                        $"Retrying in {delay.TotalMilliseconds}ms. Error: {exception.Message}"
                    );
                });
    }

    public async Task<ChannelDto> CreateAsync(CreateChannelRequest req, CancellationToken ct)
    {
        // --- Input validation ---
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Channel name is required.");

        var name = req.Name.Trim();

        if (name.Length > 100)
            throw new ArgumentException("Channel name cannot exceed 100 characters.");

        // --- Domain entity ---
        var channel = new Channel(name);

        // --- Gem i repository ---
        // Antag at repo selv håndterer duplikerede navne (evt. ved at kaste InvalidOperationException)
        await _repo.AddAsync(channel, ct);

        // --- Domain event ---
        var evt = new ChannelCreated(
            channel.Id,
            channel.Name,
            channel.CreatedAt
        );

        // 🔁 Publish med retry-policy
        await _publishRetryPolicy.ExecuteAsync(async token =>
        {
            await _bus.PublishAsync(evt, "channel.created", token);
        }, ct);

        // --- Return DTO ---
        return new ChannelDto(
            channel.Id,
            channel.Name,
            channel.CreatedAt
        );
    }
}
