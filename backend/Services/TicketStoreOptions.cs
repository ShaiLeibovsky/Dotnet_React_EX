namespace TicketApi.Services;

/// <summary>Bound from the "TicketStore" config section.</summary>
public class TicketStoreOptions
{
    public const string SectionName = "TicketStore";

    /// <summary>Path to the live JSON data file (read/write).</summary>
    public string FilePath { get; set; } = "tickets.json";

    /// <summary>Optional seed file copied in on first run if FilePath is missing.</summary>
    public string? SeedPath { get; set; }
}
