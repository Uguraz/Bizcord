using System.Net;
using System.Net.Http.Json;
using ChannelMicroservice.Contracts.Channels;
using ChannelMicroservice.Contracts.Channels.Events;
using ChannelMicroservice.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ChannelMicroservice.IntegrationTests.Channels;

public class CreateChannelTests : IClassFixture<CustomWebAppFactory>
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;

    public CreateChannelTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(); // starter din rigtige app i TestServer
    }

    [Fact]
    public async Task Post_channels_creates_and_publishes_event()
    {
        // Arrange
        var req = new CreateChannelRequest("general");

        // Act
        var resp = await _client.PostAsJsonAsync("/channels", req);

        // Assert HTTP
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<ChannelDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("general");

        // Assert event er “publiceret” via vores fake bus
        var published = _factory.Bus.PublishedEvents
            .Where(p => p.Topic == "channel.created")
            .Select(p => p.Event)
            .OfType<ChannelCreated>()
            .ToList();

        published.Should().ContainSingle();
        published[0].ChannelId.Should().Be(dto.Id);
        published[0].Name.Should().Be("general");
    }
}