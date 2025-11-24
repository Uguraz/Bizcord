using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

// Load reverse proxy configuration from appsettings.json
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// Gateway endpoint – forwards to Channel microservice
app.MapReverseProxy();

app.Run();