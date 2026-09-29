using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketApi.Configuration;

namespace TicketApi.Modules.Summaries;

/// <summary>Summarises a description with the Gemini generateContent endpoint.</summary>
public sealed class GeminiSummaryService : ISummaryService
{
    public const string BaseAddress = "https://generativelanguage.googleapis.com/";

    private const int WordTolerance = 2;

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
        var generated =
            body
                .RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()
                ?.Trim() ?? string.Empty;

        var words = generated.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries
        ).Length;
        if (words > _options.MaxWords * WordTolerance)
            throw new InvalidOperationException(
                $"Gemini returned {words} words for a summary of at most {_options.MaxWords}."
            );

        return generated;
    }

    private string Prompt(string description) =>
        $"Summarise this customer support ticket in one sentence of at most "
        + $"{_options.MaxWords} words. Reply with the summary text only.\n\n{description}";
}
