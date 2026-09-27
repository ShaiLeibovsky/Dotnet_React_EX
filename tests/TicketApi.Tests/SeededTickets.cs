using System.Text.Json;
using TicketApi.Dtos;

namespace TicketApi.Tests;

public static class SeededTickets
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<TicketDto> Load() =>
        JsonSerializer.Deserialize<List<TicketDto>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "dataset.json")),
            JsonOptions
        ) ?? throw new InvalidOperationException("dataset.json did not contain tickets.");
}
