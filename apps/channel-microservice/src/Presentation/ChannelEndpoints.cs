using ChannelMicroservice.Application.Channels;
using ChannelMicroservice.Contracts.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ChannelMicroservice.Messaging.Sagas;

namespace ChannelMicroservice.Presentation;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
    {
        // Opret en route-gruppe for alle channel-endpoints
        var group = app.MapGroup("/channels");

        // POST /channels
        group.MapPost("/", async (
                CreateChannelRequest req,
                ChannelService svc,
                ChannelCreationSaga saga,
                CancellationToken ct) =>
            {
                try
                {
                    var dto = await svc.CreateAsync(req, ct);

                    // Start Saga-workflow for den nye channel
                    await saga.HandleAsync(dto.Id, dto.Name, ct);

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

        return group;
    }
}
