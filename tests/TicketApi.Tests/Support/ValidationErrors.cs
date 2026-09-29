using System.Net.Http.Json;

namespace TicketApi.Tests.Support;

public static class ValidationErrors
{
    public static async Task<IReadOnlySet<string>> FieldNamesAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemBody>()
            ?? throw new InvalidOperationException("Response was not a validation problem.");

        return problem.Errors.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record ValidationProblemBody(Dictionary<string, string[]> Errors);
}
