using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TicketApi.Services;

/// <summary>Summarises a description with the Gemini generateContent endpoint.</summary>
public sealed class GeminiSummaryService : ISummaryService
{
    private readonly HttpClient _client;
    private readonly SummaryOptions _options;

    public GeminiSummaryService(HttpClient client, IOptions<SummaryOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> SummariseAsync(string description, CancellationToken ct = default)
    {
        var request = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = Prompt(description) } } },
            },
        };

        var response = await _client.PostAsJsonAsync(
            $"v1beta/models/{_options.Model}:generateContent",
            request,
            ct
        );
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var generated = body
            .RootElement.GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return generated?.Trim() ?? string.Empty;
    }

    private string Prompt(string description) =>
        $"Summarise this customer support ticket in one sentence of at most "
        + $"{_options.MaxWords} words. Reply with the summary text only.\n\n{description}";
}
