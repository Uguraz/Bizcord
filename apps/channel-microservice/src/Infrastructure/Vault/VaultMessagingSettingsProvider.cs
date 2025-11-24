using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ChannelMicroservice.Infrastructure.Vault;

public class VaultMessagingSettingsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public VaultMessagingSettingsProvider(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken ct = default)
    {
        var address = _config["Vault:Address"];
        var token   = _config["Vault:Token"];

        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Vault:Address is not configured.");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Vault:Token is not configured.");

        _httpClient.BaseAddress = new Uri(address);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // svarer til secret/messaging i Vault UI
        var response = await _httpClient.GetAsync("/v1/secret/data/messaging", ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);

        var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // JSON-format:
        // {
        //   "data": {
        //     "data": {
        //       "connectionString": "..."
        //     }
        //   }
        // }

        var root = json.RootElement;

        var connString = root
            .GetProperty("data")
            .GetProperty("data")
            .GetProperty("connectionString")
            .GetString();

        if (string.IsNullOrWhiteSpace(connString))
            throw new InvalidOperationException("Vault secret 'connectionString' is missing or empty.");

        return connString!;
    }
}
