using System.Text.Json;
using TicketApi.Services;

namespace TicketApi.Dtos;

/// <summary>
/// The body of POST /api/tickets, sent either as JSON or as multipart form data
/// carrying an optional image file.
/// </summary>
public record NewTicketPayload(CreateTicketRequest Request, IFormFile? Image)
{
    public static async Task<NewTicketPayload> ReadAsync(
        HttpRequest http,
        CancellationToken ct = default
    )
    {
        if (http.HasFormContentType)
        {
            var form = await http.ReadFormAsync(ct);
            return new NewTicketPayload(
                new CreateTicketRequest(
                    form["name"].ToString(),
                    form["email"].ToString(),
                    form["description"].ToString()
                ),
                form.Files["image"]
            );
        }

        try
        {
            var body = await http.ReadFromJsonAsync<CreateTicketRequest>(ct);
            return new NewTicketPayload(body ?? EmptyRequest, Image: null);
        }
        catch (JsonException)
        {
            throw ValidationException.Single("Body", "The request body is not valid JSON.");
        }
    }

    private static CreateTicketRequest EmptyRequest =>
        new(string.Empty, string.Empty, string.Empty);
}
