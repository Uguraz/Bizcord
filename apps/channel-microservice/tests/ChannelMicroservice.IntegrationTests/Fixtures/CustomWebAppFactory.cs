using System.Linq;
using ChannelMicroservice.IntegrationTests.Fixtures;
using ChannelMicroservice.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ChannelMicroservice.IntegrationTests.Fixtures;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    public TestMessageClient Bus { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // fjern eksisterende IMessageClient-registrering
            var desc = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageClient));
            if (desc != null) services.Remove(desc);

            // brug vores test-bus som singleton
            services.AddSingleton<IMessageClient>(Bus);
        });
    }
}