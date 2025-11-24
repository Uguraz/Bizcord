using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Contracts.Channels;
using ChannelMicroservice.Infrastructure.Channels;
using ChannelMicroservice.Infrastructure.Vault;
using ChannelMicroservice.Messaging;
using ChannelMicroservice.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Security.Claims;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------
// DI – services
// ------------------------------------------------------
builder.Services.AddSingleton<IChannelRepository, InMemoryChannelRepository>();
builder.Services.AddSingleton<IMessageClient, NoopMessageClient>();
builder.Services.AddScoped<ChannelService>();

// Simpel HttpClient til Vault (reliability håndteres via Polly ved startup)
builder.Services.AddHttpClient<VaultMessagingSettingsProvider>();

// ------------------------------------------------------
// API key authentication
// ------------------------------------------------------
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, SimpleApiKeyAuthHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireApiKey", policy =>
        policy.RequireAuthenticatedUser()
              .AddAuthenticationSchemes("ApiKey"));
});

// Demo API key
builder.Configuration["ApiKey"] = "super-secret-key";

// ------------------------------------------------------
// Build app
// ------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------
// Reliability: Retry policy for Vault-kald ved startup
// ------------------------------------------------------
var vaultRetryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt)
    );

// ------------------------------------------------------
// Hent Vault connection string ved startup (med retry)
// ------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var vault = scope.ServiceProvider.GetRequiredService<VaultMessagingSettingsProvider>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var connectionString = await vaultRetryPolicy.ExecuteAsync(
            () => vault.GetConnectionStringAsync()
        );

        logger.LogInformation("Loaded messaging connection string from Vault: {Conn}", connectionString);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to load messaging connection string from Vault, even after retries.");
    }
}

// ------------------------------------------------------
// Middleware
// ------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

// Offentligt health endpoint
app.MapGet("/health", () => Results.Ok(new { ok = true }));

// Channels – kræver API key
var channelRoutes = app.MapChannelEndpoints();
channelRoutes.RequireAuthorization("RequireApiKey");

app.Run();
