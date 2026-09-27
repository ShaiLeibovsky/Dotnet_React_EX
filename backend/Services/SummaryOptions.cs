namespace TicketApi.Services;

/// <summary>Bound from the "Summary" config section.</summary>
public class SummaryOptions
{
    public const string SectionName = "Summary";

    /// <summary>Gemini API key, supplied through user-secrets or the environment.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.5-flash-lite";

    public string BaseAddress { get; set; } = "https://generativelanguage.googleapis.com/";

    public int TimeoutSeconds { get; set; } = 10;

    public int MaxWords { get; set; } = 25;
}
