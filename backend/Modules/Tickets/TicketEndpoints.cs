using System.Security.Claims;
using TicketApi.Modules.Tickets.Dto;
using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Modules.Tickets;

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
            ClaimsPrincipal admin,
            ITicketService service,
            CancellationToken ct
        ) =>
        {
            var updated = await service.UpdateAsync(id, request, EmailOf(admin), ct);
            return updated is null ? NotFound(id) : TypedResults.Ok(updated);
        }).RequireAuthorization();

        return app;
    }

    private static string? EmailOf(ClaimsPrincipal admin) =>
        admin.FindFirstValue(ClaimTypes.Email) ?? admin.FindFirstValue("email");

    private static IResult NotFound(string id) =>
        TypedResults.Problem(
            title: "Ticket not found",
            detail: $"No ticket exists with id '{id}'.",
            statusCode: StatusCodes.Status404NotFound
        );
}
