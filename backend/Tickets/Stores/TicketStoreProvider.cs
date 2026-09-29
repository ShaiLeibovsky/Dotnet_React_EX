namespace TicketApi.Tickets.Stores;

/// <summary>Selects which <see cref="ITicketStore"/> implementation serves requests.</summary>
public enum TicketStoreProvider
{
    Json,
    Sqlite,
}
