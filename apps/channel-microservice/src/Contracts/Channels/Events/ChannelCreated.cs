namespace ChannelMicroservice.Contracts.Channels.Events;

public sealed record ChannelCreated(string ChannelId, string Name, DateTimeOffset CreatedAt);