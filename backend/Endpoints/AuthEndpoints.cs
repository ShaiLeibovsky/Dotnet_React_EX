using TicketApi.Dtos;
using TicketApi.Services;

namespace TicketApi.Endpoints;

/// <summary>Minimal API route group for /api/auth.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async Task<IResult> (
            LoginRequest request,
            AdminAuthService auth,
            CancellationToken ct
        ) =>
        {
            var session = await auth.SignInAsync(request, ct);
            return session is null ? InvalidCredentials() : TypedResults.Ok(session);
        });

        return app;
    }

    private static IResult InvalidCredentials() =>
        TypedResults.Problem(
            title: "Invalid credentials",
            detail: "The email or password is incorrect.",
            statusCode: StatusCodes.Status401Unauthorized
        );
}
