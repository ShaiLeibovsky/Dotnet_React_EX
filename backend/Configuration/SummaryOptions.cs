namespace TicketApi.Configuration;

/// <summary>Bound from the "Summary" config section.</summary>
public class SummaryOptions
{
    public const string SectionName = "Summary";

    /// <summary>Gemini API key, supplied through user-secrets or the environment.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.1-flash-lite";

    /// <summary>Budget for the whole summary call, retries included.</summary>
    public int TimeoutSeconds { get; set; } = 25;

    /// <summary>Budget for a single attempt within <see cref="TimeoutSeconds"/>.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 10;

    public int MaxWords { get; set; } = 25;
}
