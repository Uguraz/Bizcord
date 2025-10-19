using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Contracts.Channels;

namespace ChannelMicroservice.Presentation;

public static class ChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/channels", async (CreateChannelRequest req, ChannelService svc, CancellationToken ct) =>
            {
                try
                {
                    var dto = await svc.CreateAsync(req, ct);
                    return Results.Created($"/channels/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    // duplikeret navn
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("CreateChannel")
            .Accepts<CreateChannelRequest>("application/json")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}