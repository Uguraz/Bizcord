using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Infrastructure.Channels;
using ChannelMicroservice.Messaging;

var builder = WebApplication.CreateBuilder(args);

// DI (midlertidigt: in-memory + noop bus)
builder.Services.AddSingleton<IChannelRepository, InMemoryChannelRepository>();
builder.Services.AddSingleton<IMessageClient, NoopMessageClient>();
builder.Services.AddScoped<ChannelService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.Run();