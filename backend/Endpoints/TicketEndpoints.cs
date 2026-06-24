using TicketApi.Dtos;
using TicketApi.Services;

namespace TicketApi.Endpoints;

/// <summary>Minimal API route group for /api/tickets. Handlers stay thin.</summary>
public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapGet("/", async (ITicketService service, CancellationToken ct) =>
            TypedResults.Ok(await service.GetAllAsync(ct))
        );

        group.MapGet("/{id}", async Task<IResult> (
            string id,
            ITicketService service,
            CancellationToken ct
        ) =>
        {
            var ticket = await service.GetByIdAsync(id, ct);
            return ticket is null ? NotFound(id) : TypedResults.Ok(ticket);
        });

        group.MapPost("/", async Task<IResult> (
            CreateTicketRequest request,
            ITicketService service,
            CancellationToken ct
        ) =>
        {
            var created = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/tickets/{created.Id}", created);
        });

        group.MapPut("/{id}", async Task<IResult> (
            string id,
            UpdateTicketRequest request,
            ITicketService service,
            CancellationToken ct
        ) =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? NotFound(id) : TypedResults.Ok(updated);
        });

        // BONUS: protect PUT and the responses endpoint with [Authorize] on feature/jwt-auth.
        group.MapPost("/{id}/responses", async Task<IResult> (
            string id,
            AddResponseRequest request,
            ITicketService service,
            CancellationToken ct
        ) =>
        {
            var updated = await service.AddResponseAsync(id, request, ct);
            return updated is null ? NotFound(id) : TypedResults.Ok(updated);
        });

        return app;
    }

    private static IResult NotFound(string id) =>
        TypedResults.Problem(
            title: "Ticket not found",
            detail: $"No ticket exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound
        );
}
