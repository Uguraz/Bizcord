using System;
using ChannelMicroservice.Domain.Channels;
using FluentAssertions;
using Xunit;

namespace ChannelMicroservice.UnitTests.Domain;

public class ChannelDomainTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_throws_when_name_is_missing(string? name)
    {
        var act = () => new Channel(name!);
        act.Should().Throw<Exception>(); // ArgumentNullException eller ArgumentException
    }

    [Fact]
    public void Ctor_throws_when_name_too_long()
    {
        var tooLong = new string('x', 101); // > 100
        var act = () => new Channel(tooLong);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*1..100*");
    }

    [Fact]
    public void Ctor_trims_and_sets_properties()
    {
        var ch = new Channel("  general  ");
        ch.Name.Should().Be("general");
        ch.Id.Should().NotBeNullOrWhiteSpace();
        ch.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}