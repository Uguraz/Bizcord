namespace ChannelMicroservice.Domain.Channels;

public sealed class Channel
{
    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Channel(string name)
    {
        name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        if (name.Length is < 1 or > 100)
            throw new ArgumentException("Channel name must be 1..100 characters.", nameof(name));

        Name = name;
    }
}