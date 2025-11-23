using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Infrastructure.Channels;
using ChannelMicroservice.Messaging;
using ChannelMicroservice.Presentation;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Dependency Injection
// -------------------------------------------------------
builder.Services.AddSingleton<IChannelRepository, InMemoryChannelRepository>();
builder.Services.AddSingleton<IMessageClient, NoopMessageClient>();
builder.Services.AddScoped<ChannelService>();

var app = builder.Build();

// -------------------------------------------------------
// Simple API key auth for POST /channels
// -------------------------------------------------------
const string ApiKeyHeaderName = "X-Api-Key";
const string ApiKeyValue = "super-secret-key";

app.Use(async (context, next) =>
{
    // Beskyt kun POST /channels
    if (context.Request.Path.StartsWithSegments("/channels") &&
        HttpMethods.IsPost(context.Request.Method))
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) ||
            providedKey != ApiKeyValue)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid API key");
            return;
        }
    }

    await next();
});

// -------------------------------------------------------
// Endpoints
// -------------------------------------------------------

// Health check
app.MapGet("/health", () => Results.Ok(new { ok = true }));

// Channel endpoints (fra ChannelEndpoints.cs)
app.MapChannelEndpoints();

app.Run();
