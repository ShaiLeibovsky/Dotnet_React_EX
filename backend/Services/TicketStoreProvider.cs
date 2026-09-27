namespace TicketApi.Services;

/// <summary>Selects which <see cref="ITicketStore"/> implementation serves requests.</summary>
public enum TicketStoreProvider
{
    Json,
    Sqlite,
}
