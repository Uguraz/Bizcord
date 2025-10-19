using System;
using System.Threading;
using System.Threading.Tasks;
using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Contracts.Channels;
using ChannelMicroservice.Contracts.Channels.Events;
using ChannelMicroservice.Messaging;
using FluentAssertions;
using Moq;
using Xunit;

namespace ChannelMicroservice.UnitTests.Application;

public class ChannelServiceTests
{
    private readonly Mock<IChannelRepository> _repo = new();
    private readonly Mock<IMessageClient> _bus = new();
    private readonly ChannelService _svc;

    public ChannelServiceTests()
    {
        _svc = new ChannelService(_repo.Object, _bus.Object);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_Persists_And_Publishes_Event()
    {
        // Arrange
        var req = new CreateChannelRequest("general");
        _repo.Setup(r => r.ExistsByNameAsync("general", It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        // Act
        var dto = await _svc.CreateAsync(req, CancellationToken.None);

        // Assert DTO
        dto.Should().NotBeNull();
        dto.Name.Should().Be("general");
        dto.Id.Should().NotBeNullOrWhiteSpace();

        // Assert persistence
        _repo.Verify(r => r.AddAsync(It.IsAny<ChannelMicroservice.Domain.Channels.Channel>(),
                                     It.IsAny<CancellationToken>()),
                     Times.Once);

        // Assert event publish
        _bus.Verify(b => b.PublishAsync(
                        It.Is<ChannelCreated>(e =>
                            e.ChannelId == dto.Id &&
                            e.Name == "general"),
                        It.Is<string?>(t => t == "channel.created"),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Fails_When_Name_Is_Duplicate()
    {
        // Arrange
        var req = new CreateChannelRequest("general");
        _repo.Setup(r => r.ExistsByNameAsync("general", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        // Act
        var act = async () => await _svc.CreateAsync(req, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*unique*");

        _repo.Verify(r => r.AddAsync(It.IsAny<ChannelMicroservice.Domain.Channels.Channel>(),
                                     It.IsAny<CancellationToken>()),
                     Times.Never);
        _bus.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAsync_Fails_When_Name_Invalid(string badName)
    {
        var req = new CreateChannelRequest(badName);
        var act = async () => await _svc.CreateAsync(req, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.AddAsync(It.IsAny<ChannelMicroservice.Domain.Channels.Channel>(),
                                     It.IsAny<CancellationToken>()),
                     Times.Never);
        _bus.VerifyNoOtherCalls();
    }
}
