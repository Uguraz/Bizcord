var app = builder.Build();

// Simple API key auth for POST /channels
const string ApiKeyHeaderName = "X-Api-Key";
const string ApiKeyValue = "super-secret-key";

app.Use(async (context, next) =>
{
    // Vi beskytter kun POST /channels
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

// dine existing endpoints:
app.MapGet("/health", () => Results.Ok(new { ok = true }));

// her mapper du dine channel-endpoints
// fx ChannelEndpoints.MapChannels(app);

app.Run();
