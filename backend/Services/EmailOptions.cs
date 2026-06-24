namespace TicketApi.Services;

/// <summary>Bound from the "Email" config section.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Base URL for the customer-facing ticket tracking link.</summary>
    public string TrackingBaseUrl { get; set; } = "http://localhost:5173/tickets";
}
